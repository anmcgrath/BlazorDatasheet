using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Events;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.Layers;
using Bunit;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Web;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class DatasheetReferenceDragTests
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

    private static SheetPointerEventArgs Pointer(int row, int col) => new() { Row = row, Col = col, MouseButton = 0 };

    private static async Task<IRenderedComponent<Datasheet>> BeginFormulaEdit(BunitContext context, Sheet sheet,
        string formula)
    {
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        await component.InvokeAsync(() =>
        {
            sheet.Selection.Set(0, 0);
            component.Instance.ForceReRender();
            sheet.Editor.BeginEdit(0, 0, true, EditEntryMode.Key, "=");
            sheet.Editor.EditValue = formula;
        });
        return component;
    }

    private static IReadOnlyList<IElement> Edges(IRenderedComponent<Datasheet> component) =>
        component.FindComponents<HighlightLayer>().SelectMany(x => x.FindAll(".bds-reference-edge")).ToList();

    private static IReadOnlyList<IElement> Corners(IRenderedComponent<Datasheet> component) =>
        component.FindComponents<HighlightLayer>().SelectMany(x => x.FindAll(".bds-reference-corner")).ToList();

    [Test]
    public async Task Cell_And_Range_References_Can_Be_Dragged_But_Named_References_Cannot()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        sheet.NamedRanges.Set("myName", "E5:E6");
        var component = await BeginFormulaEdit(context, sheet, "=myName+B2+C3:D4");

        component.FindComponents<HighlightBox>().Select(x => x.Instance.CanDrag).Should().Equal(false, true, true);
        Edges(component).Should().HaveCount(8);
        Corners(component).Should().HaveCount(8);

        await component.InvokeAsync(() => sheet.Editor.FormulaEdit.IsPickingEnabled = false);
        Edges(component).Should().BeEmpty();
        Corners(component).Should().BeEmpty();
    }

    [Test]
    public async Task Parts_Of_A_Reference_That_A_Pane_Cuts_Off_Cannot_Be_Dragged()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        sheet.FreezeTopRows(2);
        var component = await BeginFormulaEdit(context, sheet, "=B1:C4");

        // The frozen pane has the top of the reference and the pane below it has the bottom. Each has both sides.
        Edges(component).Should().HaveCount(6);
        Corners(component).Should().HaveCount(4);
    }

    [Test]
    public async Task Dragging_A_Corner_Resizes_The_Reference_In_The_Formula()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=SUM(B2:C3)");

        // the corners are top left, top right, bottom left and bottom right
        await Corners(component)[3].TriggerEventAsync("onpointerdown", new PointerEventArgs());
        sheet.Editor.FormulaEdit.IsAdjustingReference.Should().BeTrue();

        await component.InvokeAsync(() => component.Instance.HandleCellMouseOver(null, Pointer(5, 6)));
        sheet.Editor.EditValue.Should().Be("=SUM(B2:G6)");
        Corners(component)[0].GetAttribute("style").Should().Contain("pointer-events:none");

        await component.InvokeAsync(() => component.Instance.HandleCellMouseOver(null, Pointer(0, 0)));
        sheet.Editor.EditValue.Should().Be("=SUM(A1:B2)");

        await component.InvokeAsync(async () =>
            (await component.Instance.HandleWindowMouseUp(new MouseEventArgs())).Should().BeTrue());

        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.FormulaEdit.IsAdjustingReference.Should().BeFalse();
        Corners(component)[0].GetAttribute("style").Should().Contain("pointer-events:all");
    }

    [Test]
    public async Task Dragging_An_Edge_Moves_The_Reference_In_The_Formula()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, "=SUM(B2:C3)");

        // the top edge, grabbed at its start, which is over B2
        await Edges(component)[0].TriggerEventAsync("onpointerdown", new PointerEventArgs { OffsetX = 1 });
        await component.InvokeAsync(() => component.Instance.HandleCellMouseOver(null, Pointer(4, 2)));

        sheet.Editor.EditValue.Should().Be("=SUM(C5:D6)");
    }

    [Test]
    public async Task A_Reference_To_Another_Sheet_Is_Dragged_On_The_Datasheet_That_Shows_It()
    {
        await using var context = CreateContext();
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        var other = workbook.AddSheet(10, 10);
        var component = await BeginFormulaEdit(context, sheet, $"={other.Name}!B2:C3");
        var otherComponent = context.Render<Datasheet>(p => p.Add(x => x.Sheet, other));

        Corners(component).Should().BeEmpty();
        await Corners(otherComponent)[3].TriggerEventAsync("onpointerdown", new PointerEventArgs());

        await otherComponent.InvokeAsync(async () =>
        {
            otherComponent.Instance.HandleCellMouseOver(null, Pointer(4, 4));
            (await otherComponent.Instance.HandleWindowMouseUp(new MouseEventArgs())).Should().BeTrue();
        });

        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Editor.EditValue.Should().Be($"={other.Name}!B2:E5");
    }
}
