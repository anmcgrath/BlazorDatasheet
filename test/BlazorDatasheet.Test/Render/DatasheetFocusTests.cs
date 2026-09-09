using System;
using System.Collections.Generic;
using BlazorDatasheet.Events;
using Microsoft.AspNetCore.Components;
using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Services;
using BlazorDatasheet.Edit.DefaultComponents;
using Bunit;
using FluentAssertions;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class DatasheetFocusTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    [Test]
    public async Task Focus_Transitions_Notify_Activation_First_And_Ignore_Duplicates_And_Stale_Events()
    {
        using var context = CreateContext();
        var events = new List<string>();
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, new Sheet(2, 2))
            .Add(x => x.OnSheetActiveChanged, e => events.Add("active:" + e.IsActive))
            .Add(x => x.OnFocusIn, _ => events.Add("in"))
            .Add(x => x.OnFocusOut, _ => events.Add("out")));
        component.Find(".bds-sheet").GetAttribute("tabindex").Should().Be("0");
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = false, Version = 2 }));
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        events.Should().Equal("active:True", "in", "active:False", "out");
    }

    [Test]
    public async Task Selection_Dims_While_Browser_Focus_Is_Elsewhere()
    {
        using var context = CreateContext();
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, new Sheet(2, 2)));
        component.Find(".bds-sheet").ClassList.Should().Contain("bds-sheet-unfocused");
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        component.Find(".bds-sheet").ClassList.Should().NotContain("bds-sheet-unfocused");
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = false, Version = 2 }));
        component.Find(".bds-sheet").ClassList.Should().Contain("bds-sheet-unfocused");
    }

    [Test]
    public async Task Manual_Activation_Does_Not_Claim_Browser_Focus()
    {
        using var context = CreateContext();
        var focusCount = 0;
        var activeCount = 0;
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, new Sheet(2, 2))
            .Add(x => x.OnSheetActiveChanged, _ => activeCount++)
            .Add(x => x.OnFocusIn, _ => focusCount++));
        await component.InvokeAsync(() => component.Instance.SetActiveAsync());
        await component.InvokeAsync(() => component.Instance.SetActiveAsync());
        activeCount.Should().Be(1);
        focusCount.Should().Be(0);
    }

    [Test]
    public async Task Focus_Loss_Preserves_Edit_And_Finishing_Inactive_Edit_Does_Not_Suppress_Keys()
    {
        using var context = CreateContext();
        var sheet = new Sheet(2, 2);
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        await component.InvokeAsync(() => sheet.Editor.BeginEdit(0, 0, true, EditEntryMode.Key, "a"));
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = false, Version = 2 }));
        sheet.Editor.IsEditing.Should().BeTrue();
        await component.InvokeAsync(() => sheet.Editor.CancelEdit());
        var state = context.JSInterop.Invocations.Where(x => x.Identifier == "setInputState").Last();
        state.Arguments[0].Should().Be(false);
        state.Arguments[1].Should().Be(false);
    }

    [Test]
    public async Task Focus_Loss_During_Activation_Callback_Drops_Obsolete_Focus_In()
    {
        using var context = CreateContext();
        var entered = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var events = new List<string>();
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, new Sheet(2, 2))
            .Add(x => x.OnSheetActiveChanged, EventCallback.Factory.Create<SheetActiveEventArgs>(this, async e =>
            {
                if (e.IsActive) { entered.SetResult(); await release.Task; }
            }))
            .Add(x => x.OnFocusIn, _ => events.Add("in"))
            .Add(x => x.OnFocusOut, _ => events.Add("out")));
        var first = component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        await entered.Task;
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = false, Version = 2 }));
        release.SetResult();
        await first;
        events.Should().Equal("out");
    }

    [Test]
    public async Task Disposed_Component_Ignores_Late_Focus_Notifications()
    {
        using var context = CreateContext();
        var count = 0;
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, new Sheet(2, 2))
            .Add(x => x.OnFocusIn, _ => count++));
        await component.InvokeAsync(() => component.Instance.DisposeAsync().AsTask());
        await component.InvokeAsync(() => component.Instance.HandleFocusChanged(new() { Focused = true, Version = 1 }));
        count.Should().Be(0);
    }
    [Test]
    public async Task Consecutive_Edits_Initialize_The_Current_Editor_With_Their_Own_Entry_Key()
    {
        using var context = CreateContext();
        var sheet = new Sheet(3, 3);
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        await component.InvokeAsync(() =>
        {
            component.Instance.ForceReRender();
            sheet.Editor.BeginEdit(0, 0, true, EditEntryMode.Key, "A");
        });
        var first = component.FindComponent<TextEditorComponent>().Instance;
        first.CurrentValue.Should().Be("A");
        await component.InvokeAsync(() =>
        {
            sheet.Editor.CancelEdit();
            sheet.Editor.BeginEdit(1, 0, true, EditEntryMode.Key, "B");
        });
        var second = component.FindComponent<TextEditorComponent>().Instance;
        second.Should().NotBeSameAs(first);
        second.CurrentValue.Should().Be("B");
    }

}
