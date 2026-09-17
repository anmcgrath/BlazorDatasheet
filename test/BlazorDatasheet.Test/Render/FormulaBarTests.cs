using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Extensions;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class FormulaBarTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    private static string ShownValue(IRenderedComponent<FormulaBar> bar) =>
        bar.FindComponent<FormulaEditor>().Instance.Value;

    private static Task Type(IRenderedComponent<FormulaBar> bar, string text) =>
        bar.InvokeAsync(() => bar.FindComponent<HighlightedInput>().Instance.HandleInput(text));

    [Test]
    public async Task Shows_The_Formula_Or_Value_Of_The_Active_Cell()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 5;
        sheet.Cells[1, 0].Formula = "=A1*2";
        var bar = context.RenderComponent<FormulaBar>(p => p.Add(x => x.Sheet, sheet));

        ShownValue(bar).Should().BeEmpty();

        await bar.InvokeAsync(() => sheet.Selection.Set(0, 0));
        ShownValue(bar).Should().Be("5");

        await bar.InvokeAsync(() => sheet.Selection.Set(1, 0));
        ShownValue(bar).Should().Be("=A1*2");

        await bar.InvokeAsync(() => sheet.Cells[1, 0].Formula = "=A1*3");
        ShownValue(bar).Should().Be("=A1*3");

        var other = new Sheet(2, 2);
        other.Cells[0, 0].Value = "other";
        other.Selection.Set(0, 0);
        bar.SetParametersAndRender(p => p.Add(x => x.Sheet, other));
        ShownValue(bar).Should().Be("other");

        await bar.InvokeAsync(() => sheet.Selection.Set(0, 0));
        ShownValue(bar).Should().Be("other", "the bar no longer listens to the first sheet");
    }

    [Test]
    public async Task Editing_From_The_Bar_Edits_The_Active_Cell_And_Enter_Moves_On()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var datasheet = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var bar = context.RenderComponent<FormulaBar>(p => p.Add(x => x.Sheet, sheet));

        context.JSInterop.Invocations.Should().Contain(x => x.Identifier == "addExternalEditor",
            "the bar is part of the sheet as far as focus goes");

        await datasheet.InvokeAsync(() => sheet.Selection.Set(1, 1));
        await bar.InvokeAsync(() => bar.Find(".bds-formula-bar").FocusIn());

        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.EditCell!.Row.Should().Be(1);
        sheet.Editor.IsSoftEdit.Should().BeFalse();
        sheet.Editor.FormulaEdit.InputOwner.Should().BeSameAs(bar.FindComponent<FormulaEditor>().Instance);

        await Type(bar, "=1+2");
        sheet.Editor.EditValue.Should().Be("=1+2");
        datasheet.FindComponent<TextEditorComponent>().Instance.CurrentValue.Should().Be("=1+2",
            "the editor in the cell mirrors the bar");

        await bar.InvokeAsync(() => bar.Find(".bds-formula-bar").KeyDown(new KeyboardEventArgs { Key = "Enter", Code = "Enter" }));

        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Cells[1, 1].Value.Should().Be(3);
        sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(2, 1));
        ShownValue(bar).Should().BeEmpty("the bar now shows the cell below");
    }

    [Test]
    public async Task Typing_In_The_Cell_Is_Mirrored_In_The_Bar()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var datasheet = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var bar = context.RenderComponent<FormulaBar>(p => p.Add(x => x.Sheet, sheet));

        await datasheet.InvokeAsync(() =>
        {
            sheet.Selection.Set(0, 0);
            sheet.Editor.BeginEdit(0, 0);
            sheet.Editor.EditValue = "=SUM(";
        });

        ShownValue(bar).Should().Be("=SUM(");

        await datasheet.InvokeAsync(() => sheet.Editor.CancelEdit());
        ShownValue(bar).Should().BeEmpty();
    }

    [Test]
    public async Task Text_Typed_When_The_Cell_Cannot_Be_Edited_Is_Put_Back()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = "fixed";
        var datasheet = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet).Add(x => x.IsReadOnly, true));
        var bar = context.RenderComponent<FormulaBar>(p => p.Add(x => x.Sheet, sheet));

        await datasheet.InvokeAsync(() => sheet.Selection.Set(0, 0));
        await bar.InvokeAsync(() => bar.Find(".bds-formula-bar").FocusIn());
        await Type(bar, "fixedx");

        sheet.Editor.IsEditing.Should().BeFalse();
        ShownValue(bar).Should().Be("fixed");
        context.JSInterop.Invocations.Last(x => x.Identifier == "setInputText").Arguments[0].Should().Be("fixed");
    }

    [Test]
    public async Task Bar_Stays_With_An_Edit_When_Another_Sheet_Of_The_Workbook_Is_Shown()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(5, 5);
        var other = workbook.AddSheet(5, 5);
        other.Cells[0, 0].Value = "other";
        other.Selection.Set(0, 0);
        var datasheet = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var bar = context.RenderComponent<FormulaBar>(p => p.Add(x => x.Sheet, sheet));

        await datasheet.InvokeAsync(() =>
        {
            sheet.Selection.Set(0, 0);
            sheet.Editor.BeginEdit(0, 0);
            sheet.Editor.EditValue = "=SUM(";
        });

        bar.SetParametersAndRender(p => p.Add(x => x.Sheet, other));
        ShownValue(bar).Should().Be("=SUM(");

        await datasheet.InvokeAsync(() => sheet.Editor.CancelEdit());
        ShownValue(bar).Should().Be("other");
    }

    [Test]
    public async Task Disposing_The_Bar_Releases_The_Sheet_And_The_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var bar = context.RenderComponent<FormulaBar>(p => p.Add(x => x.Sheet, sheet));

        await bar.InvokeAsync(() => bar.Instance.DisposeAsync().AsTask());

        context.JSInterop.Invocations.Should().Contain(x => x.Identifier == "removeExternalEditor");
    }
}
