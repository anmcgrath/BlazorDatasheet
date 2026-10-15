using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.AutoScroll;
using BlazorDatasheet.Render.Layers.Preview;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class SelectionMoveControllerTests
{
    private Sheet _sheet = null!;
    private SelectionMoveController _move = null!;
    private PreviewService _previews = null!;
    private AutoScrollState _scroll = null!;
    private bool _readOnly;
    private DragMoveKinds _allowed;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(20, 10);
        _previews = new PreviewService();
        _scroll = new AutoScrollState();
        _readOnly = false;
        _allowed = DragMoveKinds.All;
        _move = new SelectionMoveController(_sheet, () => _readOnly, () => _allowed, _previews, _scroll);
    }

    [TearDown] public void Dispose() => _move.Dispose();

    private static SheetPointerEventArgs At(int row, int col, double x = 100, double fraction = .2) => new()
    {
        Row = row, Col = col, PageX = x, IsInsideSheet = true,
        RowFraction = fraction, ColFraction = fraction
    };

    [Test]
    public void Range_Move_Overwrites_And_Follows_Formulas_With_One_Undo()
    {
        _sheet.Cells.SetValue(1, 1, "source");
        _sheet.Cells.SetValue(5, 4, "target");
        _sheet.Cells.SetFormula(0, 0, "=B2");
        _sheet.Selection.Set(new Region(1, 2, 1, 2));
        _sheet.Selection.Activate(2, 2);
        _sheet.Commands.ClearHistory();
        _move.Begin(new CellPosition(2, 2), 0, 0).Should().BeTrue();
        _move.Move(At(6, 5));
        _sheet.Cells.GetValue(1, 1).Should().Be("source");
        _previews.Items.Should().ContainSingle();
        _move.End(At(6, 5));
        _sheet.Cells.GetValue(5, 4).Should().Be("source");
        _sheet.Cells.GetValue(0, 0).Should().Be("source");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(5, 6, 4, 5));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(6, 5));
        _sheet.Commands.GetUndoCommands().Should().ContainSingle();
        _previews.Items.Should().BeEmpty();
        _scroll.IsActive.Should().BeFalse();
        _sheet.Commands.Undo().Should().BeTrue();
        _sheet.Cells.GetValue(1, 1).Should().Be("source");
        _sheet.Cells.GetValue(5, 4).Should().Be("target");
        _sheet.Commands.Redo().Should().BeTrue();
        _sheet.Cells.GetValue(5, 4).Should().Be("source");
    }

    [TestCase(true, 6, .2, 4)]
    [TestCase(true, 6, .8, 5)]
    [TestCase(true, 0, .2, 0)]
    [TestCase(false, 6, .2, 4)]
    [TestCase(false, 6, .8, 5)]
    [TestCase(false, 0, .2, 0)]
    public void Headings_Reorder_Before_Or_After_Midpoint(bool rows, int pointer, double fraction, int final)
    {
        _sheet.Cells.SetValue(2, 2, "source");
        _sheet.Selection.Set(rows ? new RowRegion(2, 3) : new ColumnRegion(2, 3));
        var press = At(rows ? 2 : -1, rows ? -1 : 2, 0);
        _move.BeginHeading(press).Should().BeTrue();
        _move.End(At(rows ? pointer : -1, rows ? -1 : pointer, 100, fraction));
        _sheet.Cells.GetValue(rows ? final : 2, rows ? 2 : final).Should().Be("source");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo<IRegion>(rows ? new RowRegion(final, final + 1) : new ColumnRegion(final, final + 1));
        _sheet.NumRows.Should().Be(20);
        _sheet.NumCols.Should().Be(10);
    }

    [Test]
    public void Heading_Click_Is_Deferred_And_Modifiers_Do_Not_Arm_Moving()
    {
        _sheet.Selection.Set(new ColumnRegion(2, 3));
        var press = At(-1, 2, 0);
        press.ShiftKey = true;
        _move.BeginHeading(press).Should().BeFalse();
        press.ShiftKey = false;
        _move.BeginHeading(press).Should().BeTrue();
        _move.End(At(-1, 2, 4)).Should().BeSameAs(press);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(2, 3));
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Cancellation_And_Outside_Release_Leave_Data_Untouched(bool outside)
    {
        _sheet.Cells.SetValue(1, 1, "source");
        _sheet.Selection.Set(1, 1);
        _sheet.Commands.ClearHistory();
        _move.Begin(new CellPosition(1, 1), 0, 0);
        _move.Move(At(3, 3));
        var end = At(3, 3);
        end.IsInsideSheet = !outside;
        end.IsCancelled = !outside;
        _move.End(end);
        _sheet.Cells.GetValue(1, 1).Should().Be("source");
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
        _previews.Items.Should().BeEmpty();
        _scroll.IsActive.Should().BeFalse();
    }

    [Test]
    public void Multiple_Selections_And_ReadOnly_Disable_Moving()
    {
        _sheet.Selection.Set(new List<IRegion> { new Region(0, 0), new Region(2, 2) });
        _move.CanBegin.Should().BeFalse();
        _sheet.Selection.Set(1, 1);
        _readOnly = true;
        _move.CanBegin.Should().BeFalse();
    }

    [Test]
    public void External_Selection_Change_Cancels_Active_Drag()
    {
        _sheet.Selection.Set(1, 1);
        _move.Begin(new CellPosition(1, 1), 0, 0);
        _move.Move(At(3, 3));
        _sheet.Selection.Set(4, 4);
        _move.IsPending.Should().BeFalse();
        _previews.Items.Should().BeEmpty();
    }

    [Test]
    public void Locked_Target_Rejects_Drop_And_Rechecks_On_Release()
    {
        _sheet.Selection.Set(1, 1);
        _move.Begin(new CellPosition(1, 1), 0, 0);
        _move.Move(At(3, 3));
        _move.IsValid.Should().BeTrue();
        _sheet.SetFormat(new Region(3, 3), new CellFormat { IsReadOnly = true });
        _sheet.Commands.ClearHistory();
        _move.End(At(3, 3));
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1));
    }

    [Test]
    public void NoOp_Drop_Adds_No_History()
    {
        _sheet.Selection.Set(new RowRegion(2, 3));
        _move.BeginHeading(At(2, -1, 0));
        _move.End(At(3, -1, 100, .8));
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [Test]
    public void Only_Allowed_Kinds_Can_Be_Moved()
    {
        _allowed = DragMoveKinds.Rows | DragMoveKinds.Columns;
        _sheet.Selection.Set(1, 1);
        _move.CanBegin.Should().BeFalse();
        _move.Begin(new CellPosition(1, 1), 0, 0).Should().BeFalse();
        _sheet.Selection.Set(new ColumnRegion(2));
        _move.CanBegin.Should().BeTrue();
        _allowed = DragMoveKinds.Ranges;
        _move.CanBegin.Should().BeFalse();
        _move.BeginHeading(At(-1, 2, 0)).Should().BeFalse();
        _sheet.Selection.Set(new RowRegion(2));
        _move.CanBegin.Should().BeFalse();
    }

    [Test]
    public void Disallowing_During_Drag_Cancels_It()
    {
        _sheet.Cells.SetValue(1, 1, "source");
        _sheet.Selection.Set(1, 1);
        _sheet.Commands.ClearHistory();
        _move.Begin(new CellPosition(1, 1), 0, 0);
        _move.Move(At(3, 3));
        _allowed = DragMoveKinds.None;
        _move.End(At(3, 3));
        _move.IsPending.Should().BeFalse();
        _sheet.Cells.GetValue(1, 1).Should().Be("source");
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [Test]
    public void BeforeRangeMove_Can_Cancel_A_Drag()
    {
        _sheet.Cells.SetValue(1, 1, "source");
        _sheet.Selection.Set(1, 1);
        _sheet.Commands.ClearHistory();
        BeforeRangeMoveEventArgs? raised = null;
        _sheet.BeforeRangeMove += (_, e) =>
        {
            raised = e;
            e.Cancel = true;
        };
        _move.Begin(new CellPosition(1, 1), 0, 0);
        _move.Move(At(3, 4));
        raised.Should().BeNull("the event is raised on release, not while dragging");
        _move.End(At(3, 4));
        raised!.IsUserDrag.Should().BeTrue();
        raised.Source.Region.Should().BeEquivalentTo(new Region(1, 1));
        raised.To.Should().Be(new CellPosition(3, 4));
        raised.ShiftAxis.Should().BeNull();
        _sheet.Cells.GetValue(1, 1).Should().Be("source");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1));
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
        _move.IsPending.Should().BeFalse();
    }

    [Test]
    public void BeforeRangeMove_Can_Redirect_A_Drag_And_The_Selection_Follows()
    {
        _sheet.Cells.SetValue(1, 1, "source");
        _sheet.Selection.Set(new Region(1, 2, 1, 2));
        _sheet.Selection.Activate(2, 2);
        _sheet.BeforeRangeMove += (_, e) => e.To = new CellPosition(7, 6);
        _move.Begin(new CellPosition(1, 1), 0, 0);
        _move.End(At(3, 4));
        _sheet.Cells.GetValue(7, 6).Should().Be("source");
        _sheet.Cells.GetValue(3, 4).Should().BeNull();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(7, 8, 6, 7));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(8, 7));
    }

    [Test]
    public void BeforeRangeMove_Gets_The_Insertion_Point_Of_A_Column_Drag()
    {
        _sheet.Cells.SetValue(0, 2, "c");
        _sheet.Selection.Set(new ColumnRegion(2));
        BeforeRangeMoveEventArgs? raised = null;
        CellPosition proposed = default;
        _sheet.BeforeRangeMove += (_, e) =>
        {
            raised = e;
            proposed = e.To;
            e.To = new CellPosition(0, 0);
        };
        _move.BeginHeading(At(-1, 2, 0)).Should().BeTrue();
        _move.End(At(-1, 6, 100, .2));
        raised!.ShiftAxis.Should().Be(Axis.Col);
        proposed.Should().Be(new CellPosition(0, 6));
        _sheet.Cells.GetValue(0, 0).Should().Be("c");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(0));
    }
}
