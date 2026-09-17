using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Events;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class DatasheetFormulaPickingTests
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

    private static SheetPointerEventArgs Pointer(int row, int col) => new() { Row = row, Col = col, MouseButton = 0 };

    private static async Task<IRenderedComponent<Datasheet>> BeginFormulaEdit(TestContext context, Sheet sheet,
        string key, Dictionary<string, CellTypeDefinition>? cellTypes = null)
    {
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet)
            .Add(x => x.CustomCellTypeDefinitions, cellTypes ?? new()));
        await component.InvokeAsync(() =>
        {
            sheet.Selection.Set(0, 0);
            component.Instance.ForceReRender();
            sheet.Editor.BeginEdit(0, 0, true, EditEntryMode.Key, key);
        });
        return component;
    }

    [Test]
    public async Task Dragging_Over_Cells_Writes_A_Reference_Into_The_Formula_Being_Edited()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=");

        await component.InvokeAsync(async () =>
        {
            component.Instance.HandleCellMouseDown(null, Pointer(1, 1));
            component.Instance.HandleCellMouseOver(null, Pointer(2, 2));
            (await component.Instance.HandleWindowMouseUp(new MouseEventArgs())).Should().BeTrue();
        });

        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.EditValue.Should().Be("=B2:C3");
        component.FindComponent<TextEditorComponent>().Instance.CurrentValue.Should().Be("=B2:C3");
        sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 0));
    }

    [Test]
    public async Task Clicking_A_Cell_Accepts_The_Edit_When_A_Reference_Cannot_Be_Picked()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "5");

        await component.InvokeAsync(async () =>
        {
            component.Instance.HandleCellMouseDown(null, Pointer(1, 1));
            await component.Instance.HandleWindowMouseUp(new MouseEventArgs());
        });

        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Cells[0, 0].Value.Should().Be(5);
        sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
    }

    [Test]
    public async Task Arrow_Keys_Pick_A_Reference_During_A_Soft_Edit()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=");

        await component.InvokeAsync(async () =>
        {
            await component.Instance.SetActiveAsync();
            (await component.Instance.HandleWindowKeyDown(new KeyboardEventArgs { Key = "ArrowDown" }))
                .Should().BeTrue();
        });

        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.EditValue.Should().Be("=A2");
    }

    [Test]
    public async Task Custom_Editor_Is_Offered_Pointer_Input_Before_Reference_Picking()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Type = "greedy";
        var component = await BeginFormulaEdit(context, sheet, "=", new()
        {
            { "greedy", new CellTypeDefinition(typeof(GreedyEditor), typeof(TextRenderer)) }
        });

        var editor = component.FindComponent<GreedyEditor>().Instance;
        await component.InvokeAsync(() => component.Instance.HandleCellMouseDown(null, Pointer(1, 1)));

        editor.MouseDownCount.Should().Be(1);
        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.EditValue.Should().Be("=");
    }

    private static List<string> GetHighlightColors(IRenderedComponent<Datasheet> component) =>
        component.FindComponent<BlazorDatasheet.Render.Layers.HighlightLayer>()
            .FindComponents<BoxOverlayRenderer>()
            .Select(x => x.Instance.BorderColor)
            .ToList();

    [Test]
    public async Task Sheet_Highlights_Use_The_Same_Colors_As_The_Formula_Text()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        sheet.NamedRanges.Set("myName", "B2:B3");
        var component = await BeginFormulaEdit(context, sheet, "=");

        await component.InvokeAsync(() => sheet.Editor.EditValue = "=myName+A1+LOG10(C3)");

        sheet.Editor.FormulaEdit.References.Select(x => x.ColorIndex).Should().Equal(1, 2, 3);
        GetHighlightColors(component).Should().Equal(
            "var(--highlight-color-1)", "var(--highlight-color-2)", "var(--highlight-color-3)");

        context.JSInterop.Invocations.Last(x => x.Identifier == "setHighlightHtml").Arguments[0]!.ToString()
            .Should().Contain("color:var(--highlight-color-1)\">myName<")
            .And.Contain("color:var(--highlight-color-3)\">C3<");
    }

    [Test]
    public async Task References_To_Other_Sheets_Are_Not_Highlighted_But_Keep_Their_Color()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=");

        await component.InvokeAsync(() => sheet.Editor.EditValue = "=Other!A1+B2");
        GetHighlightColors(component).Should().Equal("var(--highlight-color-2)");

        await component.InvokeAsync(() => sheet.Editor.CancelEdit());
        GetHighlightColors(component).Should().BeEmpty();
    }

    [Test]
    public async Task Dragging_Over_Another_Sheet_In_The_Workbook_Writes_A_Reference_To_It()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        var other = workbook.AddSheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=");
        var otherComponent = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, other));
        await otherComponent.InvokeAsync(() => other.Selection.Set(5, 5));

        await otherComponent.InvokeAsync(async () =>
        {
            otherComponent.Instance.HandleCellMouseDown(null, Pointer(1, 1));
            otherComponent.Instance.HandleCellMouseOver(null, Pointer(2, 2));
            // the mouse up is on the window, which every datasheet hears
            await component.Instance.HandleWindowMouseUp(new MouseEventArgs());
            await otherComponent.Instance.HandleWindowMouseUp(new MouseEventArgs());
        });

        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.EditValue.Should().Be("=Sheet2!B2:C3");
        sheet.Editor.FormulaEdit.IsDragging.Should().BeFalse();
        other.Selection.ActiveCellPosition.Should().Be(new CellPosition(5, 5));

        GetHighlightColors(component).Should().BeEmpty();
        GetHighlightColors(otherComponent).Should().Equal("var(--highlight-color-1)");

        await component.InvokeAsync(() => sheet.Editor.EditValue = "=Sheet2!B2:C3+A1");
        GetHighlightColors(component).Should().Equal("var(--highlight-color-2)");
        GetHighlightColors(otherComponent).Should().Equal("var(--highlight-color-1)");
    }

    [Test]
    public async Task Clicking_Another_Sheet_Accepts_The_Edit_When_A_Reference_Cannot_Be_Picked()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        var other = workbook.AddSheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=");
        var otherComponent = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, other));
        await component.InvokeAsync(() => sheet.Editor.EditValue = "=5");

        await otherComponent.InvokeAsync(async () =>
        {
            otherComponent.Instance.HandleCellMouseDown(null, Pointer(1, 1));
            await otherComponent.Instance.HandleWindowMouseUp(new MouseEventArgs());
        });

        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Cells[0, 0].Value.Should().Be(5);
        other.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
    }

    private class GreedyEditor : BaseEditor
    {
        public int MouseDownCount { get; private set; }

        public override void BeforeEdit(IReadOnlyCell cell, Sheet sheet)
        {
            sheet.Editor.FormulaEdit.IsPickingEnabled = true;
        }

        public override void BeginEdit(EditEntryMode entryMode, string? editValue, string key)
        {
            CurrentValue = key;
        }

        public override bool HandleMouseDown(int row, int col, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey)
        {
            MouseDownCount++;
            return true;
        }
    }
}
