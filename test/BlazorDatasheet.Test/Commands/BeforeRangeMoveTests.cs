using AwesomeAssertions;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.DataStructures.Geometry;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class BeforeRangeMoveTests
{
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup() => _sheet = new Sheet(10, 10);

    [Test]
    public void Cells_Move_Raises_The_Event_With_The_Move()
    {
        BeforeRangeMoveEventArgs? raised = null;
        _sheet.BeforeRangeMove += (_, e) => raised = e;
        _sheet.Cells.SetValue(1, 1, "a");
        _sheet.Cells.Move(new Region(1, 2, 1, 2), new CellPosition(4, 4), Axis.Row).Should().BeTrue();
        raised!.Source.Sheet.Should().BeSameAs(_sheet);
        raised.Source.Region.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
        raised.To.Should().Be(new CellPosition(4, 4));
        raised.ShiftAxis.Should().Be(Axis.Row);
        raised.IsUserDrag.Should().BeFalse();
    }

    [Test]
    public void Cancelling_Leaves_The_Sheet_And_History_Untouched()
    {
        _sheet.BeforeRangeMove += (_, e) => e.Cancel = true;
        _sheet.Cells.SetValue(1, 1, "a");
        _sheet.Commands.ClearHistory();
        _sheet.Cells.Move(new Region(1, 1), new CellPosition(4, 4)).Should().BeFalse();
        _sheet.Rows.Move(1, 1, 5).Should().BeFalse();
        _sheet.Columns.Move(1, 1, 5).Should().BeFalse();
        _sheet.Cells.GetValue(1, 1).Should().Be("a");
        _sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [Test]
    public void Changing_To_And_ShiftAxis_Redirects_The_Move()
    {
        _sheet.BeforeRangeMove += (_, e) =>
        {
            e.To = new CellPosition(6, 6);
            e.ShiftAxis = null;
        };
        _sheet.Cells.SetValue(1, 1, "a");
        _sheet.Cells.SetValue(1, 2, "b");
        _sheet.Cells.Move(new Region(1, 1), new CellPosition(1, 5), Axis.Col).Should().BeTrue();
        _sheet.Cells.GetValue(6, 6).Should().Be("a");
        // overwritten rather than shifted, so b stays where it was
        _sheet.Cells.GetValue(1, 2).Should().Be("b");
        _sheet.Cells.GetValue(1, 1).Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Row_And_Column_Moves_Raise_The_Insertion_Point_And_Can_Be_Redirected(bool rows)
    {
        BeforeRangeMoveEventArgs? raised = null;
        CellPosition proposed = default;
        _sheet.BeforeRangeMove += (_, e) =>
        {
            raised = e;
            proposed = e.To;
            e.To = new CellPosition(0, 0);
        };
        _sheet.Cells.SetValue(rows ? 3 : 0, rows ? 0 : 3, "x");
        var store = rows ? (RowColInfoStore)_sheet.Rows : _sheet.Columns;
        store.Move(3, 1, 7).Should().BeTrue();
        raised!.Source.Region.Should().BeEquivalentTo<IRegion>(rows ? new RowRegion(3) : new ColumnRegion(3));
        proposed.Should().Be(rows ? new CellPosition(7, 0) : new CellPosition(0, 7));
        raised.ShiftAxis.Should().Be(rows ? Axis.Row : Axis.Col);
        _sheet.Cells.GetValue(0, 0).Should().Be("x");
        _sheet.Commands.Undo().Should().BeTrue();
        _sheet.Cells.GetValue(rows ? 3 : 0, rows ? 0 : 3).Should().Be("x");
    }
}
