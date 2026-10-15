using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.KeyboardInput;
using BlazorDatasheet.Virtualise;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class DatasheetSelectionMoveTests
{
    private static BunitContext Context()
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
    public async Task Border_Move_Uses_Shared_Handles_And_Commits_On_Release()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(new Region(1, 2, 1, 2));
        sheet.Cells.SetValue(1, 1, "move me");
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        component.FindAll(".bds-selection-edge").Should().HaveCount(4);
        component.FindAll(".bds-reference-corner").Should().BeEmpty();
        await component.FindAll(".bds-selection-edge")[0].TriggerEventAsync("onpointerdown",
            new PointerEventArgs { OffsetX = 1, PageX = 100, PageY = 100 });
        await component.InvokeAsync(() => component.Instance.HandleSelectionDragMove(null,
            new SheetPointerEventArgs { Row = 4, Col = 3, PageX = 200, PageY = 200, IsInsideSheet = true }));
        sheet.Cells.GetValue(1, 1).Should().Be("move me");
        await component.InvokeAsync(() => component.Instance.HandleSelectionDragEnd(null,
            new SheetPointerEventArgs { Row = 4, Col = 3, PageX = 200, PageY = 200, IsInsideSheet = true }));
        sheet.Cells.GetValue(4, 3).Should().Be("move me");
        sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(4, 5, 3, 4));
    }

    [Test]
    public async Task Escape_Cancels_And_Selected_Heading_Click_Still_Selects_One_Column()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(new ColumnRegion(1, 2));
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var press = new SheetPointerEventArgs { Row = -1, Col = 1, PageX = 100, IsInsideSheet = true };
        await component.InvokeAsync(() => component.Instance.HandleCellMouseDown(null, press));
        await component.InvokeAsync(() => component.Instance.HandleSelectionDragMove(null,
            new SheetPointerEventArgs { Row = -1, Col = 5, PageX = 400, IsInsideSheet = true }));
        await component.InvokeAsync(async () =>
            (await component.Instance.HandleShortcuts("Escape", KeyboardModifiers.None)).Should().BeTrue());
        sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(1, 2));
        await component.InvokeAsync(() => component.Instance.HandleCellMouseDown(null, press));
        await component.InvokeAsync(() => component.Instance.HandleSelectionDragEnd(null, press));
        sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(1));
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [Test]
    public async Task Heading_Dropdown_Press_Selects_The_Column_Without_Starting_A_Move()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(new ColumnRegion(1, 2));
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        var press = new SheetPointerEventArgs { Row = -1, Col = 2, PageX = 100, IsInsideSheet = true, IsOnChrome = true };
        await component.InvokeAsync(() => component.Instance.HandleCellMouseDown(null, press));
        await component.InvokeAsync(() => component.Instance.HandleWindowMouseUp(new MouseEventArgs()));
        sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(2));

        await component.InvokeAsync(() => component.Instance.HandleCellMouseDown(null, new SheetPointerEventArgs { Row = -1, Col = 5, PageX = 300, IsInsideSheet = true, IsOnChrome = true }));
        await component.InvokeAsync(() => component.Instance.HandleWindowMouseUp(new MouseEventArgs()));
        sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(5));
    }

    [Test]
    public async Task ReadOnly_And_Multiple_Selections_Have_No_Move_Handles()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(1, 1);
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet).Add(x => x.IsReadOnly, true));
        component.FindAll(".bds-selection-edge").Should().BeEmpty();
        component.Render(p => p.Add(x => x.IsReadOnly, false));
        await component.InvokeAsync(() => sheet.Selection.Set([new Region(1, 1), new Region(3, 3)]));
        component.FindAll(".bds-selection-edge").Should().BeEmpty();
    }

    [Test]
    public async Task Selected_Headings_Show_Grab_Cursor_Only_When_Movable()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(new ColumnRegion(1, 2));
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(component);
        component.FindAll(".bds-col-head.bds-movable-header").Select(e => e.GetAttribute("data-col"))
            .Should().BeEquivalentTo(["1", "2"]);
        component.FindAll(".bds-row-head.bds-movable-header").Should().BeEmpty();

        await component.InvokeAsync(() => sheet.Selection.Set(new RowRegion(3)));
        component.FindAll(".bds-row-head.bds-movable-header").Select(e => e.GetAttribute("data-row"))
            .Should().BeEquivalentTo(["3"]);
        component.FindAll(".bds-col-head.bds-movable-header").Should().BeEmpty();

        await component.InvokeAsync(() => sheet.Selection.Set([new RowRegion(3), new RowRegion(5)]));
        component.FindAll(".bds-movable-header").Should().BeEmpty();

        await component.InvokeAsync(() => sheet.Selection.Set(new RowRegion(3)));
        component.Render(p => p.Add(x => x.IsReadOnly, true));
        component.FindAll(".bds-movable-header").Should().BeEmpty();
    }

    [Test]
    public async Task Selected_Headings_Lose_Grab_Cursor_While_Editing()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(new ColumnRegion(1, 2));
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(component);
        component.FindAll(".bds-movable-header").Should().HaveCount(2);

        await component.InvokeAsync(() => sheet.Editor.BeginEdit(0, 1));
        component.WaitForAssertion(() => component.FindAll(".bds-movable-header").Should().BeEmpty());

        await component.InvokeAsync(() => sheet.Editor.CancelEdit());
        component.WaitForAssertion(() => component.FindAll(".bds-movable-header").Should().HaveCount(2));

        await component.InvokeAsync(() => sheet.Editor.BeginEdit(0, 1));
        await component.InvokeAsync(() => sheet.Editor.AcceptEdit());
        component.WaitForAssertion(() => component.FindAll(".bds-movable-header").Should().HaveCount(2));
    }

    private static void ShowViewport(IRenderedComponent<Datasheet> component)
    {
        foreach (var virtualiser in component.FindComponents<Virtualise2D>())
            component.InvokeAsync(() => virtualiser.Instance.HandleScroll(new Rect(0, 0, 500, 500))).Wait();
    }

    [Test]
    public async Task AllowDragMove_Limits_What_Has_Move_Handles()
    {
        await using var context = Context();
        var sheet = new Sheet(10, 10);
        sheet.Selection.Set(1, 1);
        var component = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet)
            .Add(x => x.AllowDragMove, DragMoveKinds.Columns));
        ShowViewport(component);
        component.FindAll(".bds-selection-edge").Should().BeEmpty();
        await component.InvokeAsync(() => sheet.Selection.Set(new ColumnRegion(2)));
        component.FindAll(".bds-selection-edge").Should().NotBeEmpty();
        component.FindAll(".bds-movable-header").Should().NotBeEmpty();
        component.Render(p => p.Add(x => x.AllowDragMove, DragMoveKinds.None));
        component.FindAll(".bds-selection-edge").Should().BeEmpty();
        component.FindAll(".bds-movable-header").Should().BeEmpty();
        var press = new SheetPointerEventArgs { Row = -1, Col = 2, PageX = 100, IsInsideSheet = true };
        await component.InvokeAsync(() => component.Instance.HandleCellMouseDown(null, press));
        await component.InvokeAsync(() => component.Instance.HandleSelectionDragEnd(null,
            new SheetPointerEventArgs { Row = -1, Col = 6, PageX = 400, IsInsideSheet = true }));
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }
}
