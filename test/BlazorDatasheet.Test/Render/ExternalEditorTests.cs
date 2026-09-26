using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The parts of the datasheet that editors outside of it rely on.
/// </summary>
public class ExternalEditorTests
{
    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    [Test]
    public async Task Datasheets_Showing_A_Sheet_Are_Found_From_The_Sheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(3, 3);
        var other = new Sheet(3, 3);
        var changes = 0;
        DatasheetRegistry.For(sheet).Changed += () => changes++;

        DatasheetRegistry.For(sheet).Active.Should().BeNull();

        var first = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var second = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        changes.Should().Be(2);
        DatasheetRegistry.For(sheet).Datasheets.Should().Equal(first.Instance, second.Instance);
        DatasheetRegistry.For(sheet).Active.Should().BeSameAs(second.Instance);

        await first.InvokeAsync(() => first.Instance.SetActiveAsync());
        DatasheetRegistry.For(sheet).Active.Should().BeSameAs(first.Instance);

        first.Render(p => p.Add(x => x.Sheet, other));
        DatasheetRegistry.For(sheet).Datasheets.Should().Equal(second.Instance);
        DatasheetRegistry.For(other).Active.Should().BeSameAs(first.Instance);

        await second.InvokeAsync(() => second.Instance.DisposeAsync().AsTask());
        DatasheetRegistry.For(sheet).Active.Should().BeNull();
    }

    [Test]
    public async Task Edit_Begun_And_Finished_From_Outside_Behaves_As_It_Does_In_The_Sheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        await component.InvokeAsync(async () =>
        {
            (await component.Instance.BeginEditActiveCellAsync()).Should().BeFalse("nothing is selected");
            sheet.Selection.Set(1, 1);
            (await component.Instance.BeginEditActiveCellAsync()).Should().BeTrue();
        });

        sheet.Editor.EditCell!.Row.Should().Be(1);
        sheet.Editor.IsSoftEdit.Should().BeFalse();

        await component.InvokeAsync(async () =>
        {
            sheet.Editor.EditValue = "=1+2";
            (await component.Instance.HandleExternalEditorKeyAsync(new KeyboardEventArgs { Key = "Enter", Code = "Enter" }))
                .Should().BeTrue();
        });

        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Cells[1, 1].Value.Should().Be(3);
        sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(2, 1));
        context.JSInterop.Invocations.Should().Contain(x => x.Identifier == "restoreFocus");

        await component.InvokeAsync(async () =>
        {
            await component.Instance.BeginEditActiveCellAsync();
            sheet.Editor.EditValue = "abc";
            await component.Instance.HandleExternalEditorKeyAsync(new KeyboardEventArgs { Key = "Escape", Code = "Escape" });
        });

        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Cells[2, 1].Value.Should().BeNull();
    }

    [Test]
    public async Task Read_Only_Datasheet_Does_Not_Begin_An_Edit_From_Outside()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet).Add(x => x.IsReadOnly, true));

        await component.InvokeAsync(async () =>
        {
            sheet.Selection.Set(1, 1);
            (await component.Instance.BeginEditActiveCellAsync()).Should().BeFalse();
        });

        sheet.Editor.IsEditing.Should().BeFalse();
    }

    [Test]
    public async Task External_Editor_Elements_Are_Registered_With_The_Window_Events()
    {
        await using var context = CreateContext();
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, new Sheet(2, 2)));

        await component.InvokeAsync(() => component.Instance.RegisterExternalEditorAsync(default));
        await component.InvokeAsync(() => component.Instance.UnregisterExternalEditorAsync(default));

        context.JSInterop.Invocations.Select(x => x.Identifier)
            .Where(x => x.EndsWith("ExternalEditor"))
            .Should().Equal("addExternalEditor", "removeExternalEditor");
    }
}
