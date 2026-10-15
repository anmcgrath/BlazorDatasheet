using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Validation;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MoveRowsColsCommandTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet("Sheet1", 20, 5);
    }

    private record RowState(
        object? Value,
        double Height,
        double VisualHeight,
        bool Visible,
        string? Heading,
        TextAlign? Alignment,
        string? Group,
        string? Background);

    private List<RowState> Rows() =>
        Enumerable.Range(0, _sheet.NumRows)
            .Select(i => new RowState(
                _sheet.Cells.GetValue(i, 0),
                _sheet.Rows.GetPhysicalHeight(i),
                _sheet.Rows.GetVisualHeight(i),
                _sheet.Rows.IsVisible(i),
                _sheet.Rows.GetHeading(i),
                _sheet.Rows.GetHeadingAlignment(i),
                _sheet.Rows.GetGroup(i)?.Group.Label,
                _sheet.GetFormat(i, 1).BackgroundColor))
            .ToList();

    private void SetupRows()
    {
        for (var r = 0; r < 12; r++)
            _sheet.Cells.SetValue(r, 0, $"r{r}");
        _sheet.Rows.SetSizesImpl(1, 1, 50);
        _sheet.Rows.SetSizesImpl(2, 2, 60);
        _sheet.Rows.SetHeadingsImpl(1, 1, "h1");
        _sheet.Rows.SetHeadingAlignmentImpl(2, 2, TextAlign.Center);
        _sheet.Rows.HideImpl(2, 1);
        _sheet.Rows.SetGroupImpl(1, 2, "g");
        _sheet.Rows.SetFormatImpl(new CellFormat { BackgroundColor = "red" }, 1, 2);
    }

    [Test]
    public void Move_Rows_Down_Carries_Everything()
    {
        SetupRows();
        var before = Rows();

        _sheet.Rows.Move(1, 2, 9).Should().BeTrue();

        _sheet.NumRows.Should().Be(20);
        var after = Rows();
        // rows 1:2 land at 7:8, rows 3..8 move up to 1..6
        after[7].Should().Be(before[1]);
        after[8].Should().Be(before[2]);
        for (var r = 3; r <= 8; r++)
            after[r - 2].Should().Be(before[r]);
        after[0].Should().Be(before[0]);
        after.Skip(9).Should().Equal(before.Skip(9));

        after[7].Should().Be(new RowState("r1", 50, 50, true, "h1", null, "g", "red"));
        after[8].Should().Be(new RowState("r2", 60, 0, false, null, TextAlign.Center, "g", "red"));
        _sheet.Rows.GetGroups().Select(g => (g.Start, g.End, g.Group.Label))
            .Should().Equal((7, 8, "g"));
        _sheet.Rows.Formats.Get(7)!.BackgroundColor.Should().Be("red");
        _sheet.Rows.Formats.Get(1).Should().BeNull();
    }

    [Test]
    public void Move_Rows_Up()
    {
        SetupRows();
        var before = Rows();

        _sheet.Rows.Move(1, 2, 0).Should().BeTrue();

        var after = Rows();
        after[0].Should().Be(before[1]);
        after[1].Should().Be(before[2]);
        after[2].Should().Be(before[0]);
        after.Skip(3).Should().Equal(before.Skip(3));
        _sheet.NumRows.Should().Be(20);
    }

    [Test]
    public void Move_Rows_Up_From_Further_Down()
    {
        SetupRows();
        var before = Rows();

        _sheet.Rows.Move(5, 2, 1).Should().BeTrue();

        var after = Rows();
        after[0].Should().Be(before[0]);
        after[1].Should().Be(before[5]);
        after[2].Should().Be(before[6]);
        for (var r = 1; r <= 4; r++)
            after[r + 2].Should().Be(before[r]);
        after.Skip(7).Should().Equal(before.Skip(7));
    }

    [TestCase(1, 2, 1)]
    [TestCase(1, 2, 3)]
    [TestCase(1, 0, 5)]
    public void No_Op_Moves_Return_False(int start, int count, int to)
    {
        SetupRows();
        var before = Rows();
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        _sheet.Rows.Move(start, count, to).Should().BeFalse();

        Rows().Should().Equal(before);
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount);
    }

    [Test]
    public void Moving_Into_Itself_Is_Refused()
    {
        SetupRows();
        _sheet.Rows.Move(1, 3, 2).Should().BeFalse();
    }

    [Test]
    public void References_Follow_The_Moved_Rows()
    {
        for (var r = 0; r < 12; r++)
            _sheet.Cells.SetValue(r, 0, r + 1);
        _sheet.Cells.SetFormula(15, 0, "=SUM(A2:A3)");
        _sheet.Cells.SetFormula(16, 0, "=SUM(A1:A20)");
        _sheet.Cells.SetFormula(17, 0, "=A2");
        _sheet.Cells.SetFormula(18, 0, "=A5");
        _sheet.Cells.SetFormula(19, 1, "=SUM(2:3)");
        var sheet2 = _workbook.AddSheet("Sheet2", 5, 5);
        sheet2.Cells.SetFormula(0, 0, "=Sheet1!A2:A3");
        sheet2.Cells.SetFormula(1, 0, "=Sheet1!A2");

        _sheet.Rows.Move(1, 2, 9).Should().BeTrue();

        _sheet.Cells.GetFormulaString(15, 0).Should().Be("=SUM(A8:A9)");
        _sheet.Cells.GetValue(15, 0).Should().Be(5);
        _sheet.Cells.GetFormulaString(16, 0).Should().Be("=SUM(A1:A20)");
        _sheet.Cells.GetFormulaString(17, 0).Should().Be("=A8");
        _sheet.Cells.GetValue(17, 0).Should().Be(2);
        _sheet.Cells.GetFormulaString(18, 0).Should().Be("=A3");
        _sheet.Cells.GetValue(18, 0).Should().Be(5);
        _sheet.Cells.GetFormulaString(19, 1).Should().Be("=SUM(8:9)");
        sheet2.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!A8:A9");
        sheet2.Cells.GetFormulaString(1, 0).Should().Be("=Sheet1!A8");
        sheet2.Cells.GetValue(1, 0).Should().Be(2);

        _sheet.Cells.SetValue(7, 0, 100);
        _sheet.Cells.GetValue(17, 0).Should().Be(100);
        sheet2.Cells.GetValue(1, 0).Should().Be(100);
    }

    [Test]
    public void Undo_And_Redo()
    {
        SetupRows();
        _sheet.Cells.SetFormula(15, 0, "=SUM(A2:A3)");
        var before = Rows();
        var formulaBefore = _sheet.Cells.GetFormulaString(15, 0);
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        _sheet.Rows.Move(1, 2, 9).Should().BeTrue();
        var after = Rows();
        var formulaAfter = _sheet.Cells.GetFormulaString(15, 0);
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount + 1);

        _sheet.Commands.Undo();
        Rows().Should().Equal(before);
        _sheet.Cells.GetFormulaString(15, 0).Should().Be(formulaBefore);
        _sheet.Rows.GetGroups().Select(g => (g.Start, g.End, g.Group.Label)).Should().Equal((1, 2, "g"));
        _sheet.NumRows.Should().Be(20);

        _sheet.Commands.Redo();
        Rows().Should().Equal(after);
        _sheet.Cells.GetFormulaString(15, 0).Should().Be(formulaAfter);

        _sheet.Commands.Undo();
        Rows().Should().Equal(before);
    }

    [Test]
    public void Move_Columns_Carries_Widths_Headings_Formats_And_Filters()
    {
        var sheet = _workbook.AddSheet("Cols", 5, 10);
        for (var c = 0; c < 6; c++)
            sheet.Cells.SetValue(0, c, $"c{c}");
        sheet.Columns.SetSizesImpl(1, 1, 200);
        sheet.Columns.SetHeadingsImpl(1, 1, "h1");
        sheet.Columns.SetFormatImpl(new CellFormat { BackgroundColor = "blue" }, 1, 1);
        var filter = new PatternFilter(PatternFilterType.Contains, "x");
        sheet.Columns.Filters.SetImpl(1, [filter]);
        sheet.Cells.SetFormula(4, 9, "=B1");

        sheet.Columns.Move(1, 1, 4).Should().BeTrue();

        sheet.NumCols.Should().Be(10);
        Enumerable.Range(0, 6).Select(c => sheet.Cells.GetValue(0, c))
            .Should().Equal("c0", "c2", "c3", "c1", "c4", "c5");
        sheet.Columns.GetPhysicalWidth(3).Should().Be(200);
        sheet.Columns.GetPhysicalWidth(1).Should().Be(sheet.Columns.DefaultSize);
        sheet.Columns.GetHeading(3).Should().Be("h1");
        sheet.Columns.GetHeading(1).Should().BeNull();
        sheet.GetFormat(2, 3).BackgroundColor.Should().Be("blue");
        sheet.GetFormat(2, 1).BackgroundColor.Should().BeNull();
        sheet.Columns.Filters.Get(3).Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
        sheet.Columns.Filters.Get(1).Filters.Should().BeEmpty();
        sheet.Cells.GetFormulaString(4, 9).Should().Be("=D1");

        sheet.Commands.Undo();
        Enumerable.Range(0, 6).Select(c => sheet.Cells.GetValue(0, c))
            .Should().Equal("c0", "c1", "c2", "c3", "c4", "c5");
        sheet.Columns.GetPhysicalWidth(1).Should().Be(200);
        sheet.Columns.Filters.Get(1).Filters.Should().ContainSingle();
        sheet.Columns.Filters.Get(3).Filters.Should().BeEmpty();
        sheet.Cells.GetFormulaString(4, 9).Should().Be("=B1");
    }

    [Test]
    public void Move_Columns_Left()
    {
        var sheet = _workbook.AddSheet("Cols", 5, 10);
        for (var c = 0; c < 6; c++)
            sheet.Cells.SetValue(0, c, $"c{c}");

        sheet.Columns.Move(3, 2, 1).Should().BeTrue();

        Enumerable.Range(0, 6).Select(c => sheet.Cells.GetValue(0, c))
            .Should().Equal("c0", "c3", "c4", "c1", "c2", "c5");
    }

    [Test]
    public void Validators_And_Metadata_Move_With_Rows()
    {
        var validator = new NumberValidator(true);
        _sheet.Validators.Add(new BlazorDatasheet.DataStructures.Geometry.Region(1, 0), validator);
        _sheet.Cells.SetCellMetaData(1, 0, "m", "x");

        _sheet.Rows.Move(1, 1, 5).Should().BeTrue();

        _sheet.Validators.Get(4, 0).Should().Equal(validator);
        _sheet.Validators.Get(1, 0).Should().BeEmpty();
        _sheet.Cells.GetMetaData(4, 0, "m").Should().Be("x");

        _sheet.Commands.Undo();
        _sheet.Validators.Get(1, 0).Should().Equal(validator);
        _sheet.Validators.Get(4, 0).Should().BeEmpty();
        _sheet.Cells.GetMetaData(1, 0, "m").Should().Be("x");
        _sheet.Cells.GetMetaData(4, 0, "m").Should().BeNull();
    }

    [Test]
    public void Named_Range_Moves_With_Rows()
    {
        _sheet.Cells.SetValue(1, 0, 7);
        _sheet.NamedRanges.Set("nm", "A2");
        _sheet.Cells.SetFormula(10, 4, "=nm");

        _sheet.Rows.Move(1, 1, 5).Should().BeTrue();

        _sheet.Cells.GetValue(4, 0).Should().Be(7);
        _sheet.NamedRanges.GetRangeString("nm").Should().Be("Sheet1!A5");
        _sheet.Cells.GetValue(10, 4).Should().Be(7);

        _sheet.Commands.Undo();
        _sheet.NamedRanges.GetRangeString("nm").Should().Be("Sheet1!A2");
        _sheet.Cells.GetValue(10, 4).Should().Be(7);
    }
}
