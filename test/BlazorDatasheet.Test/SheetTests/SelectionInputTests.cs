using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SelectionInputTests
{
    private Sheet _sheet = null!;
    private SelectionInputManager _manager = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(10, 10);
        _manager = new SelectionInputManager(_sheet.Selection);
        _sheet.Selection.Set(new Region(1, 2, 1, 2));
    }

    private void Input(string input)
    {
        switch (input)
        {
            case "click": _manager.HandlePointerDown(3, 3, false, false, false, 0); break;
            case "right click": _manager.HandlePointerDown(3, 3, false, false, false, 2); break;
            case "shift click": _manager.HandlePointerDown(3, 3, true, false, false, 0); break;
            case "shift arrow": _manager.HandleArrowKeyDown(true, new Offset(0, 1)); break;
            case "arrow": _manager.HandleArrowKeyDown(false, new Offset(0, 1)); break;
            case "tab": _manager.HandleTabEnterNavigation(Axis.Col, 1); break;
            case "enter": _manager.HandleTabEnterNavigation(Axis.Row, 1); break;
            case "row": _manager.HandlePointerDown(3, -1, false, false, false, 0); break;
            case "column": _manager.HandlePointerDown(-1, 3, false, false, false, 0); break;
            case "group": _manager.HandleHeaderSelection(new ColumnRegion(3, 4)); break;
            default: throw new ArgumentException(input);
        }
    }

    [TestCase("click", SelectionInputKind.PointerStart)]
    [TestCase("right click", SelectionInputKind.PointerStart)]
    [TestCase("shift click", SelectionInputKind.ShiftExtension)]
    [TestCase("shift arrow", SelectionInputKind.ShiftExtension)]
    [TestCase("arrow", SelectionInputKind.ArrowNavigation)]
    [TestCase("tab", SelectionInputKind.TabEnterNavigation)]
    [TestCase("enter", SelectionInputKind.TabEnterNavigation)]
    [TestCase("row", SelectionInputKind.HeaderSelection)]
    [TestCase("column", SelectionInputKind.HeaderSelection)]
    [TestCase("group", SelectionInputKind.HeaderSelection)]
    public void Canceling_Input_Preserves_State_Without_Notifications(string input, SelectionInputKind kind)
    {
        var notifications = 0;
        _sheet.Selection.SelectionChanged += (_, _) => notifications++;
        _sheet.Selection.SelectingChanged += (_, _) => notifications++;
        _sheet.Selection.CellsSelected += (_, _) => notifications++;
        _sheet.Selection.ActiveCellPositionChanged += (_, _) => notifications++;
        _sheet.Selection.ActiveRegionChanged += (_, _) => notifications++;
        _sheet.Selection.BeforeActiveCellPositionChanged += (_, _) => notifications++;
        var calls = 0;
        _sheet.BeforeSelectionInput += (sender, e) =>
        {
            sender.Should().BeSameAs(_sheet);
            calls++;
            e.InputKind.Should().Be(kind);
            _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
            _sheet.Selection.IsSelecting.Should().BeFalse();
            notifications.Should().Be(0);
            e.Cancel = true;
        };

        Input(input);
        _manager.HandleWindowMouseUp();

        calls.Should().Be(1);
        notifications.Should().Be(0);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [TestCase("click")]
    [TestCase("right click")]
    [TestCase("shift click")]
    [TestCase("shift arrow")]
    [TestCase("arrow")]
    [TestCase("tab")]
    [TestCase("enter")]
    [TestCase("row")]
    [TestCase("column")]
    [TestCase("group")]
    public void Consumer_Can_Replace_Complete_Proposal(string input)
    {
        var calls = 0;
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            calls++;
            e.ProposedRegions = new IRegion[] { new Region(5, 6, 5, 6), new Region(8, 8) };
            e.ProposedActiveRegionIndex = 0;
            e.ProposedActiveCellPosition = new CellPosition(6, 6);
        };

        Input(input);
        _manager.HandleWindowMouseUp();

        calls.Should().Be(1);
        _sheet.Selection.Regions.Should().HaveCount(2);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(5, 6, 5, 6));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(6, 6));
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [Test]
    public void Drag_Rejects_Forbidden_Cells_And_Resumes_From_Last_Accepted_Preview()
    {
        var calls = 0;
        var previews = 0;
        _sheet.Selection.SelectingChanged += (_, _) => previews++;
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            calls++;
            e.IsDragPreview.Should().BeTrue();
            e.Cancel = e.ProposedRegions.Any(x => x.Contains(2, 4));
        };
        _manager.HandlePointerDown(2, 2, false, false, false, 0);
        _manager.HandlePointerOver(3, 3);
        var acceptedPreviews = previews;
        _manager.HandlePointerOver(3, 6);

        previews.Should().Be(acceptedPreviews);
        _sheet.Selection.SelectingRegion.Should().BeEquivalentTo(new Region(2, 3, 2, 3));
        _manager.HandlePointerOver(4, 3);
        _manager.HandleWindowMouseUp();

        calls.Should().Be(4);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(2, 4, 2, 3));
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [Test]
    public void Consumer_Can_Clamp_Drag_Preview_And_Retained_Arguments_Are_Detached()
    {
        BeforeSelectionInputEventArgs? retained = null;
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            if (e.InputKind != SelectionInputKind.Drag)
                return;
            retained = e;
            e.CurrentSelectingRegion!.Shift(5, 5);
            _sheet.Selection.SelectingRegion.Should().BeEquivalentTo(new Region(1, 1));
            e.ProposedRegions = new[] { e.ProposedRegions[0].GetIntersection(new Region(0, 3, 0, 3))! };
        };
        _manager.HandlePointerDown(1, 1, false, false, false, 0);
        _manager.HandlePointerOver(8, 8);
        _sheet.Selection.SelectingRegion.Should().BeEquivalentTo(new Region(1, 3, 1, 3));
        retained!.ProposedRegions[0].Shift(4, 4);
        _manager.HandleWindowMouseUp();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 3, 1, 3));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void Additive_Click_Preserves_Other_Regions_Throughout_Drag(bool ctrl, bool meta)
    {
        _sheet.BeforeSelectionInput += (_, e) => e.ProposedRegions.Should().HaveCount(2);
        _manager.HandlePointerDown(5, 5, false, ctrl, meta, 0);
        _sheet.Selection.Regions.Should().ContainSingle().Which.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
        _manager.HandlePointerOver(6, 6);
        _manager.HandleWindowMouseUp();
        _sheet.Selection.Regions.Should().HaveCount(2);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(5, 6, 5, 6));
    }

    [TestCase("out of bounds")]
    [TestCase("partial merge")]
    [TestCase("active index")]
    [TestCase("active cell")]
    [TestCase("null list")]
    [TestCase("null region")]
    [TestCase("row bounds")]
    [TestCase("column bounds")]
    [TestCase("empty preview")]
    public void Invalid_Replacement_Is_Rejected(string invalid)
    {
        _sheet.Cells.Merge(new Region(5, 6, 5, 6));
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            switch (invalid)
            {
                case "out of bounds": e.ProposedRegions = new[] { new Region(-1, 2, 0, 2) }; break;
                case "partial merge": e.ProposedRegions = new[] { new Region(5, 5) }; e.ProposedActiveCellPosition = new CellPosition(5, 5); break;
                case "active index": e.ProposedActiveRegionIndex = 10; break;
                case "active cell": e.ProposedActiveCellPosition = new CellPosition(8, 8); break;
                case "null list": e.ProposedRegions = null!; break;
                case "null region": e.ProposedRegions = new IRegion[] { null! }; break;
                case "row bounds": e.ProposedRegions = new[] { new RowRegion(9, 10) }; break;
                case "column bounds": e.ProposedRegions = new[] { new ColumnRegion(-1, 1) }; break;
                case "empty preview": e.ProposedRegions = Array.Empty<IRegion>(); e.ProposedActiveRegionIndex = -1; break;
            }
        };
        Input("click");
        _manager.HandleWindowMouseUp();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [Test]
    public void Snapshots_Do_Not_Expose_Live_Regions()
    {
        BeforeSelectionInputEventArgs? retained = null;
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            retained = e;
            e.CurrentRegions[0].Shift(5, 5);
            e.ProposedRegions[0].Should().BeEquivalentTo(new Region(1, 2, 1, 3));
            _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
        };
        Input("shift arrow");
        retained!.ProposedRegions[0].Shift(5, 5);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 3));
    }

    [Test]
    public void Arrow_Proposal_Contains_Final_Moved_Region_And_Existing_Cell_Hook_Runs_After_Acceptance()
    {
        var order = new List<string>();
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            order.Add("input");
            e.ProposedRegions.Should().ContainSingle().Which.Should().BeEquivalentTo(new Region(1, 2));
            e.ProposedActiveCellPosition.Should().Be(new CellPosition(1, 2));
        };
        _sheet.Selection.BeforeActiveCellPositionChanged += (_, _) => order.Add("active");
        Input("arrow");
        order.Should().Equal("input", "active");
    }

    [Test]
    public void Expanding_Region_Does_Not_Raise_Active_Cell_Hook()
    {
        _sheet.BeforeSelectionInput += (_, _) => { };
        _sheet.Selection.BeforeActiveCellPositionChanged += (_, _) => Assert.Fail("Active cell is not changing");
        Input("shift arrow");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 3));
    }

    [Test]
    public void Proposals_Respect_Merges_Hidden_Cells_And_Bounds()
    {
        _sheet.Selection.Set(0, 0);
        _sheet.Rows.Hide(1, 2);
        _sheet.Columns.Hide(1, 2);
        _sheet.Cells.Merge(new Region(3, 4, 3, 4));
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            e.ProposedRegions.All(x => _sheet.Region.Contains(x)).Should().BeTrue();
        };
        _manager.HandleArrowKeyDown(false, new Offset(1, 0));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(3, 0));
        _manager.HandleArrowKeyDown(false, new Offset(0, 1));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(3, 4, 3, 4));
        _manager.HandleArrowKeyDown(true, new Offset(0, 1));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(3, 4, 3, 5));
        _sheet.Selection.Set(9, 9);
        _manager.HandleArrowKeyDown(true, new Offset(1, 0));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(9, 9));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Headers_Keep_Row_And_Column_Region_Semantics(bool row)
    {
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            if (row)
                e.ProposedRegions[0].Should().BeOfType<RowRegion>();
            else
                e.ProposedRegions[0].Should().BeOfType<ColumnRegion>();
        };
        Input(row ? "row" : "column");
        _manager.HandlePointerOver(row ? 5 : -1, row ? -1 : 5);
        _manager.HandleWindowMouseUp();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(row ? (IRegion)new RowRegion(3, 5) : new ColumnRegion(3, 5));
    }

    [Test]
    public void Direct_Api_And_Formula_Reference_Selection_Bypass_Hook()
    {
        _sheet.BeforeSelectionInput += (_, _) => Assert.Fail("Input hook should be bypassed");
        _sheet.Selection.Set(5, 5);
        _sheet.Selection.ExtendTo(6, 6);
        _sheet.Selection.BeginSelectingCell(7, 7);
        _sheet.Selection.UpdateSelectingEndPosition(8, 8);
        _sheet.Selection.EndSelecting();
        _sheet.Selection.MoveActivePositionByCol(1);

        var formulaSelection = new Selection(_sheet);
        var formulaManager = new SelectionInputManager(formulaSelection);
        formulaManager.HandlePointerDown(3, 3, false, false, false, 0);
        formulaManager.HandlePointerOver(4, 4);
        formulaManager.HandleWindowMouseUp();
        formulaManager.HandleArrowKeyDown(true, new Offset(0, 1));
        formulaSelection.ActiveRegion.Should().BeEquivalentTo(new Region(3, 4, 3, 5));
    }

    [Test]
    public void Mouse_Up_After_Unsubscribing_Commits_Last_Accepted_Replacement()
    {
        EventHandler<BeforeSelectionInputEventArgs> handler = (_, e) =>
        {
            e.ProposedRegions = new[] { new Region(5, 5) };
            e.ProposedActiveCellPosition = new CellPosition(5, 5);
        };
        _sheet.BeforeSelectionInput += handler;
        Input("click");
        _sheet.BeforeSelectionInput -= handler;
        _manager.HandleWindowMouseUp();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(5, 5));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(5, 5));
    }

    [Test]
    public void Programmatic_Clear_During_Preview_Does_Not_Restore_Removed_Regions()
    {
        _sheet.BeforeSelectionInput += (_, _) => { };
        _manager.HandlePointerDown(5, 5, false, true, false, 0);
        _sheet.Selection.ClearSelections();
        _manager.HandleWindowMouseUp();
        _sheet.Selection.Regions.Should().ContainSingle().Which.Should().BeEquivalentTo(new Region(5, 5));
    }

    [Test]
    public void Additive_Drag_Does_Not_Notify_Unchanged_Committed_Selection()
    {
        var regionChanges = 0;
        var selectionChanges = 0;
        var cellChanges = 0;
        _sheet.Selection.ActiveRegionChanged += (_, _) => regionChanges++;
        _sheet.Selection.SelectionChanged += (_, _) => selectionChanges++;
        _sheet.Selection.ActiveCellPositionChanged += (_, _) => cellChanges++;
        _sheet.BeforeSelectionInput += (_, _) => { };
        _manager.HandlePointerDown(5, 5, false, true, false, 0);
        _manager.HandlePointerOver(6, 6);
        regionChanges.Should().Be(0);
        selectionChanges.Should().Be(0);
        cellChanges.Should().Be(0);
        _manager.HandleWindowMouseUp();
        regionChanges.Should().Be(1);
        selectionChanges.Should().Be(1);
        cellChanges.Should().Be(1);
    }

    [Test]
    public void Drag_Preserves_Consumer_Region_Ordering()
    {
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            if (e.InputKind == SelectionInputKind.PointerStart)
            {
                e.ProposedRegions = e.ProposedRegions.Reverse().ToArray();
                e.ProposedActiveRegionIndex = 0;
            }
            else
            {
                e.ProposedActiveRegionIndex.Should().Be(0);
                e.ProposedRegions[1].Should().BeEquivalentTo(new Region(1, 2, 1, 2));
            }
        };
        _manager.HandlePointerDown(5, 5, false, true, false, 0);
        _manager.HandlePointerOver(6, 6);
        _manager.HandleWindowMouseUp();
        _sheet.Selection.Regions[0].Should().BeEquivalentTo(new Region(5, 6, 5, 6));
        _sheet.Selection.ActiveRegion.Should().BeSameAs(_sheet.Selection.Regions[0]);
    }

    [TestCase("click")]
    [TestCase("right click")]
    [TestCase("shift click")]
    [TestCase("shift arrow")]
    [TestCase("arrow")]
    [TestCase("tab")]
    [TestCase("enter")]
    [TestCase("row")]
    [TestCase("column")]
    [TestCase("group")]
    public void Observing_Input_Without_Modification_Preserves_Existing_Result(string input)
    {
        var expected = new Sheet(10, 10);
        expected.Selection.Set(new Region(1, 2, 1, 2));
        var manager = _manager;
        _manager = new SelectionInputManager(expected.Selection);
        Input(input);
        _manager.HandleWindowMouseUp();
        _manager = manager;
        _sheet.BeforeSelectionInput += (_, _) => { };
        Input(input);
        _manager.HandleWindowMouseUp();

        _sheet.Selection.Regions.Should().BeEquivalentTo(expected.Selection.Regions, x => x.WithStrictOrdering());
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(expected.Selection.ActiveRegion);
        _sheet.Selection.ActiveCellPosition.Should().Be(expected.Selection.ActiveCellPosition);
    }

    [Test]
    public void Tab_And_Enter_Cycle_Between_Regions_With_Hooks_Enabled()
    {
        _sheet.Selection.Set(new List<IRegion> { new Region(1, 1), new Region(3, 3) });
        _sheet.Selection.Activate(3, 3);
        _sheet.BeforeSelectionInput += (_, _) => { };
        _manager.HandleTabEnterNavigation(Axis.Col, 1);
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1));
        _manager.HandleTabEnterNavigation(Axis.Row, -1);
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(3, 3));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(3, 3));
    }

    [Test]
    public void Reverse_Drag_And_Shift_Contraction_Preserve_Anchor()
    {
        _sheet.BeforeSelectionInput += (_, _) => { };
        _manager.HandlePointerDown(5, 5, false, false, false, 0);
        _manager.HandlePointerOver(2, 2);
        _manager.HandleWindowMouseUp();
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(5, 5));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(2, 5, 2, 5));
        _manager.HandleArrowKeyDown(true, new Offset(0, 1));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(2, 5, 3, 5));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(5, 5));
    }

    [Test]
    public void Throwing_Handler_Does_Not_Mutate_Selection()
    {
        _sheet.BeforeSelectionInput += (_, _) => throw new InvalidOperationException("Consumer failure");
        Action input = () => Input("arrow");
        input.Should().Throw<InvalidOperationException>();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
    }
}
