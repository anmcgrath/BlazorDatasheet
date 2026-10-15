using System.Linq;
using System.Text;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MoveRangeWithShiftCommandTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;
    private Sheet _sheet2 = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet("Sheet1", 10, 10);
        _sheet2 = _workbook.AddSheet("Sheet2", 10, 10);
    }

    private bool Move(string from, string to, Axis axis, Sheet? target = null)
    {
        target ??= _sheet;
        var toRange = target.Range(to)!;
        return target.Cells.Move(_sheet.Range(from)!, toRange.Region.TopLeft, axis);
    }

    private void SetRowValues(Sheet sheet, int row, params object?[] values)
    {
        for (var c = 0; c < values.Length; c++)
        {
            if (values[c] is string s && s.StartsWith('='))
                sheet.Cells.SetFormula(row, c, s);
            else
                sheet.Cells.SetValue(row, c, values[c]);
        }
    }

    private static object?[] RowValues(Sheet sheet, int row, int count) =>
        Enumerable.Range(0, count).Select(c => sheet.Cells.GetValue(row, c)).ToArray();

    private static object?[] ColValues(Sheet sheet, int col, int count) =>
        Enumerable.Range(0, count).Select(r => sheet.Cells.GetValue(r, col)).ToArray();

    private static string Snapshot(params Sheet[] sheets)
    {
        var sb = new StringBuilder();
        foreach (var sheet in sheets)
        {
            for (var r = 0; r < sheet.NumRows; r++)
            for (var c = 0; c < sheet.NumCols; c++)
            {
                sb.Append(sheet.Name).Append(r).Append(',').Append(c).Append(':')
                    .Append(sheet.Cells.GetValue(r, c)).Append('|')
                    .Append(sheet.Cells.GetFormulaString(r, c)).Append('|')
                    .Append(sheet.GetFormat(r, c).BackgroundColor).Append('|')
                    .Append(sheet.Cells.GetCellType(r, c)).Append('|')
                    .Append(sheet.Cells.GetMerge(r, c))
                    .AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Rows 1:2 hold 1..5 and 10, 20, 30, 40, 50. Row 9 holds references and Sheet2!A1 references Sheet1.
    /// </summary>
    private void SetupBlock(int formulaCol, string formula)
    {
        SetRowValues(_sheet, 0, 1, 2, 3, 4, 5);
        SetRowValues(_sheet, 1, 10, 20, 30, 40, 50);
        _sheet.Cells.SetFormula(1, formulaCol, formula);
    }

    private void SetRefs(params string[] formulas)
    {
        for (var c = 0; c < formulas.Length; c++)
            _sheet.Cells.SetFormula(8, c, formulas[c]);
    }

    private string?[] RefStrings(int count) =>
        Enumerable.Range(0, count).Select(c => _sheet.Cells.GetFormulaString(8, c)).ToArray();

    private object?[] RefValues(int count) => RowValues(_sheet, 8, count);

    [Test]
    public void Move_After_Source_In_Same_Band()
    {
        // B1 cut, inserted at D1: a, c, b, d
        SetupBlock(1, "=B1+A2");
        SetRefs("=B1", "=C1", "=D1", "=E1", "=SUM(A1:E1)");
        _sheet2.Cells.SetFormula(0, 0, "=Sheet1!B1");

        Move("B1:B2", "D1", Axis.Col).Should().BeTrue();

        RowValues(_sheet, 0, 6).Should().Equal(1, 3, 2, 4, 5, null);
        RowValues(_sheet, 1, 6).Should().Equal(10, 30, 12, 40, 50, null);
        _sheet.Cells.GetFormulaString(1, 2).Should().Be("=C1+A2");

        RefStrings(5).Should().Equal("=C1", "=B1", "=D1", "=E1", "=SUM(A1:E1)");
        RefValues(5).Should().Equal(2, 3, 4, 5, 15);
        _sheet2.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!C1");
        _sheet2.Cells.GetValue(0, 0).Should().Be(2);

        _sheet.Cells.SetValue(0, 2, 7);
        _sheet.Cells.GetValue(8, 0).Should().Be(7);
        _sheet.Cells.GetValue(1, 2).Should().Be(17);
        _sheet2.Cells.GetValue(0, 0).Should().Be(7);
    }

    [Test]
    public void Move_Before_Source_In_Same_Band()
    {
        // D1 cut, inserted at B1: a, d, b, c
        SetupBlock(3, "=D1+A2");
        SetRefs("=D1", "=B1", "=C1", "=E1", "=SUM(A1:E1)");
        _sheet2.Cells.SetFormula(0, 0, "=Sheet1!D1");

        Move("D1:D2", "B1", Axis.Col).Should().BeTrue();

        RowValues(_sheet, 0, 6).Should().Equal(1, 4, 2, 3, 5, null);
        RowValues(_sheet, 1, 6).Should().Equal(10, 14, 20, 30, 50, null);
        _sheet.Cells.GetFormulaString(1, 1).Should().Be("=B1+A2");

        RefStrings(5).Should().Equal("=B1", "=C1", "=D1", "=E1", "=SUM(A1:E1)");
        RefValues(5).Should().Equal(4, 2, 3, 5, 15);
        _sheet2.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!B1");
        _sheet2.Cells.GetValue(0, 0).Should().Be(4);
    }

    [Test]
    public void Move_To_Different_Band()
    {
        SetupBlock(1, "=B1+A2");
        SetRowValues(_sheet, 4, 100, 200, 300);
        SetRefs("=B1", "=C1", "=B5", "=C5", "=SUM(A1:E1)");
        _sheet2.Cells.SetFormula(0, 0, "=Sheet1!B1");

        Move("B1:B2", "B5", Axis.Col).Should().BeTrue();

        RowValues(_sheet, 0, 5).Should().Equal(1, 3, 4, 5, null);
        RowValues(_sheet, 1, 5).Should().Equal(10, 30, 40, 50, null);
        RowValues(_sheet, 4, 5).Should().Equal(100, 2, 200, 300, null);
        RowValues(_sheet, 5, 5).Should().Equal(new object?[] { null, 12, null, null, null });
        _sheet.Cells.GetFormulaString(5, 1).Should().Be("=B5+A2");

        RefStrings(5).Should().Equal("=B5", "=B1", "=C5", "=D5", "=SUM(A1:D1)");
        RefValues(5).Should().Equal(2, 3, 200, 300, 13);
        _sheet2.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!B5");
        _sheet2.Cells.GetValue(0, 0).Should().Be(2);
    }

    [Test]
    public void Move_To_Different_Sheet_Updates_References_On_Every_Sheet()
    {
        var sheet3 = _workbook.AddSheet("Sheet3", 10, 10);
        SetupBlock(1, "=B1+A2");
        SetRefs("=B1", "=C1", "=SUM(A1:E1)");
        SetRowValues(_sheet2, 0, "x", "y", "z");
        _sheet2.Cells.SetFormula(8, 0, "=B1");
        sheet3.Cells.SetFormula(0, 0, "=Sheet1!B1");

        var undoCount = _sheet2.Commands.GetUndoCommands().Count();
        Move("B1:B2", "B1", Axis.Col, _sheet2).Should().BeTrue();

        RowValues(_sheet, 0, 5).Should().Equal(1, 3, 4, 5, null);
        RowValues(_sheet, 1, 5).Should().Equal(10, 30, 40, 50, null);
        RowValues(_sheet2, 0, 5).Should().Equal("x", 2, "y", "z", null);
        RowValues(_sheet2, 1, 3).Should().Equal(new object?[] { null, 12, null });
        _sheet2.Cells.GetFormulaString(1, 1).Should().Be("=B1+Sheet1!A2");

        RefStrings(3).Should().Equal("=Sheet2!B1", "=B1", "=SUM(A1:D1)");
        RefValues(3).Should().Equal(2, 3, 13);
        _sheet2.Cells.GetFormulaString(8, 0).Should().Be("=C1");
        _sheet2.Cells.GetValue(8, 0).Should().Be("y");
        sheet3.Cells.GetFormulaString(0, 0).Should().Be("=Sheet2!B1");
        sheet3.Cells.GetValue(0, 0).Should().Be(2);

        _sheet2.Commands.GetUndoCommands().Count().Should().Be(undoCount + 1);
        _sheet2.Commands.Undo();
        RowValues(_sheet, 0, 5).Should().Equal(1, 2, 3, 4, 5);
        RowValues(_sheet2, 0, 4).Should().Equal("x", "y", "z", null);
        sheet3.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!B1");
        RefStrings(3).Should().Equal("=B1", "=C1", "=SUM(A1:E1)");
        _sheet.Cells.GetFormulaString(1, 1).Should().Be("=B1+A2");
        _sheet.Cells.GetValue(1, 1).Should().Be(12);
    }

    [Test]
    public void Shift_Down_After_Source()
    {
        for (var r = 0; r < 5; r++)
            _sheet.Cells.SetValue(r, 0, r + 1);
        _sheet.Cells.SetFormula(0, 3, "=A2");
        _sheet.Cells.SetFormula(1, 3, "=A3");
        _sheet.Cells.SetFormula(2, 3, "=SUM(A1:A5)");

        Move("A2", "A4", Axis.Row).Should().BeTrue();

        ColValues(_sheet, 0, 6).Should().Equal(1, 3, 2, 4, 5, null);
        _sheet.Cells.GetFormulaString(0, 3).Should().Be("=A3");
        _sheet.Cells.GetFormulaString(1, 3).Should().Be("=A2");
        _sheet.Cells.GetFormulaString(2, 3).Should().Be("=SUM(A1:A5)");
        ColValues(_sheet, 3, 3).Should().Equal(2, 3, 15);
    }

    [Test]
    public void Shift_Down_Before_Source()
    {
        for (var r = 0; r < 5; r++)
            _sheet.Cells.SetValue(r, 0, r + 1);
        _sheet.Cells.SetFormula(0, 3, "=A4");
        _sheet.Cells.SetFormula(1, 3, "=A2");

        Move("A4", "A2", Axis.Row).Should().BeTrue();

        ColValues(_sheet, 0, 6).Should().Equal(1, 4, 2, 3, 5, null);
        _sheet.Cells.GetFormulaString(0, 3).Should().Be("=A2");
        _sheet.Cells.GetFormulaString(1, 3).Should().Be("=A3");
        ColValues(_sheet, 3, 2).Should().Equal(4, 2);
    }

    [TestCase("B1", "B1")]
    [TestCase("B1", "C1")]
    [TestCase("B1:C1", "D1")]
    [TestCase("B1:C1", "B1")]
    public void No_Op_Moves_Return_False(string from, string to)
    {
        SetRowValues(_sheet, 0, 1, 2, 3, 4);
        var before = Snapshot(_sheet);
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        Move(from, to, Axis.Col).Should().BeFalse();

        Snapshot(_sheet).Should().Be(before);
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount);
    }

    [TestCase("B1:C1", "C1")] // inside the block
    [TestCase("B1:B2", "D2")] // bands partly overlap
    [TestCase("B1:B2", "K1")] // runs off the sheet
    public void Invalid_Moves_Are_Refused(string from, string to)
    {
        SetRowValues(_sheet, 0, 1, 2, 3, 4);
        var before = Snapshot(_sheet);
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        Move(from, to, Axis.Col).Should().BeFalse();

        Snapshot(_sheet).Should().Be(before);
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount);
    }

    [Test]
    public void Failed_Remove_Rolls_Back_The_Insert_And_Move()
    {
        SetupBlock(1, "=B1+A2");
        SetRowValues(_sheet, 4, 100, 200, 300);
        _sheet.Cells.SetFormula(8, 0, "=B1");
        // the remove refuses read-only cells after the source in its band; the insert and move are allowed.
        _sheet.SetFormat(new Region(0, 7), new CellFormat { IsReadOnly = true });
        var before = Snapshot(_sheet, _sheet2);
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        var cmd = new MoveRangeWithShiftCommand(_sheet.Range("B1:B2")!, _sheet.Range("B5")!, Axis.Col);
        cmd.CanExecute(_sheet).Should().BeTrue();
        _sheet.Commands.ExecuteCommand(cmd).Should().BeFalse();

        Snapshot(_sheet, _sheet2).Should().Be(before);
        _sheet.Cells.GetFormulaString(8, 0).Should().Be("=B1");
        _sheet.Cells.GetValue(8, 0).Should().Be(2);
        cmd.FinalRegion.Should().BeNull();
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount);
    }

    [Test]
    public void Undo_Is_One_Step_And_Redo_Repeats()
    {
        SetupBlock(1, "=B1+A2");
        SetRefs("=B1", "=C1", "=SUM(A1:E1)");
        _sheet.SetFormat(new Region(0, 1), new CellFormat { BackgroundColor = "red" });
        _sheet2.Cells.SetFormula(0, 0, "=Sheet1!B1");
        var before = Snapshot(_sheet, _sheet2);
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        Move("B1:B2", "D1", Axis.Col).Should().BeTrue();
        var after = Snapshot(_sheet, _sheet2);
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount + 1);

        _sheet.Commands.Undo();
        Snapshot(_sheet, _sheet2).Should().Be(before);

        _sheet.Commands.Redo();
        Snapshot(_sheet, _sheet2).Should().Be(after);

        _sheet.Commands.Undo();
        Snapshot(_sheet, _sheet2).Should().Be(before);
        _sheet.Commands.Redo();
        Snapshot(_sheet, _sheet2).Should().Be(after);
    }

    [Test]
    public void Formats_Types_And_Merges_Move_With_The_Block()
    {
        SetRowValues(_sheet, 0, 1, 2, 3, 4);
        _sheet.SetFormat(new Region(0, 1), new CellFormat { BackgroundColor = "red" });
        _sheet.SetFormat(new Region(0, 2), new CellFormat { BackgroundColor = "blue" });
        _sheet.Cells.SetType(new Region(1, 1), "boolean");
        _sheet.Cells.Merge(new Region(0, 1, 1, 1));

        Move("B1:B2", "D1", Axis.Col).Should().BeTrue();

        _sheet.GetFormat(0, 2).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(0, 1).BackgroundColor.Should().Be("blue");
        _sheet.GetFormat(0, 3).BackgroundColor.Should().BeNull();
        _sheet.Cells.GetCellType(1, 2).Should().Be("boolean");
        _sheet.Cells.GetCellType(1, 1).Should().Be("default");
        _sheet.Cells.GetMerge(0, 2).Should().BeEquivalentTo(new Region(0, 1, 2, 2));
        _sheet.Cells.GetMerge(0, 1).Should().BeNull();
        _sheet.Cells.GetValue(0, 2).Should().Be(2);
    }

    [Test]
    public void Spill_Inside_The_Block_Moves_And_Respills()
    {
        _sheet.Cells.SetValue(0, 0, "a");
        _sheet.Cells.SetFormula(0, 1, "={1;2}"); // B1 spills B1:B2
        _sheet.Cells.SetValue(0, 2, "c");
        _sheet.Cells.SetValue(1, 2, "c2");
        _sheet.Cells.SetValue(0, 3, "d");

        Move("B1:B2", "D1", Axis.Col).Should().BeTrue();

        RowValues(_sheet, 0, 4).Should().Equal("a", "c", 1, "d");
        RowValues(_sheet, 1, 4).Should().Equal(new object?[] { null, "c2", 2, null });
        _sheet.Cells.GetSpillRegionForAnchor(0, 2).Should().BeEquivalentTo(new Region(0, 1, 2, 2));
        _sheet.Cells.IsSpillAnchor(0, 1).Should().BeFalse();

        _sheet.Commands.Undo();
        RowValues(_sheet, 0, 4).Should().Equal("a", 1, "c", "d");
        RowValues(_sheet, 1, 4).Should().Equal(new object?[] { null, 2, "c2", null });
        _sheet.Cells.GetSpillRegionForAnchor(0, 1).Should().BeEquivalentTo(new Region(0, 1, 1, 1));
    }

    [Test]
    public void Final_Region_Is_Where_The_Block_Lands()
    {
        SetRowValues(_sheet, 0, 1, 2, 3, 4);
        var after = new MoveRangeWithShiftCommand(_sheet.Range("B1")!, _sheet.Range("D1")!, Axis.Col);
        _sheet.Commands.ExecuteCommand(after).Should().BeTrue();
        after.FinalRegion.Should().BeEquivalentTo(new Region(0, 2));

        var before = new MoveRangeWithShiftCommand(_sheet.Range("D1")!, _sheet.Range("B1")!, Axis.Col);
        _sheet.Commands.ExecuteCommand(before).Should().BeTrue();
        before.FinalRegion.Should().BeEquivalentTo(new Region(0, 1));
    }

    [Test]
    public void Move_Without_Shift_Overwrites()
    {
        SetRowValues(_sheet, 0, 1, 2, 3, 4);
        _sheet.Cells.Move(new Region(0, 1), new CellPosition(0, 3)).Should().BeTrue();
        RowValues(_sheet, 0, 4).Should().Equal(1, null, 3, 2);
    }

    [Test]
    public void Move_Within_A_Full_Row_Takes_Nothing_Off_The_Sheet()
    {
        for (var c = 0; c < 10; c++)
            _sheet.Cells.SetValue(0, c, ((char)('a' + c)).ToString());
        var before = Snapshot(_sheet);

        Move("D1", "B1", Axis.Col).Should().BeTrue();

        RowValues(_sheet, 0, 10).Should().Equal("a", "d", "b", "c", "e", "f", "g", "h", "i", "j");
        _sheet.NumCols.Should().Be(10);
        _sheet.Commands.Undo();
        Snapshot(_sheet).Should().Be(before);
        _sheet.NumCols.Should().Be(10);
    }

    [Test]
    public void Move_After_Source_Within_A_Full_Row_Takes_Nothing_Off_The_Sheet()
    {
        for (var c = 0; c < 10; c++)
            _sheet.Cells.SetValue(0, c, ((char)('a' + c)).ToString());
        var before = Snapshot(_sheet);

        Move("B1", "E1", Axis.Col).Should().BeTrue();

        RowValues(_sheet, 0, 10).Should().Equal("a", "c", "d", "b", "e", "f", "g", "h", "i", "j");
        _sheet.Commands.Undo();
        Snapshot(_sheet).Should().Be(before);
    }

    [Test]
    public void Move_Last_Cell_Of_A_Row()
    {
        _sheet.Cells.SetValue(0, 0, "a");
        _sheet.Cells.SetValue(0, 1, "b");
        _sheet.Cells.SetValue(0, 9, "j");
        var before = Snapshot(_sheet);

        Move("J1", "B1", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetValue(0, 0).Should().Be("a");
        _sheet.Cells.GetValue(0, 1).Should().Be("j");
        _sheet.Cells.GetValue(0, 2).Should().Be("b");
        _sheet.Cells.GetValue(0, 9).Should().BeNull();
        _sheet.Commands.Undo();
        Snapshot(_sheet).Should().Be(before);
    }

    [Test]
    public void Move_Last_Cell_Of_A_Column()
    {
        _sheet.Cells.SetValue(0, 0, "a");
        _sheet.Cells.SetValue(9, 0, "j");
        var before = Snapshot(_sheet);

        Move("A10", "A1", Axis.Row).Should().BeTrue();

        ColValues(_sheet, 0, 3).Should().Equal("j", "a", null);
        _sheet.NumRows.Should().Be(10);
        _sheet.Commands.Undo();
        Snapshot(_sheet).Should().Be(before);
        _sheet.NumRows.Should().Be(10);
    }

    [Test]
    public void Formats_At_The_Edge_Of_The_Band_Are_Kept()
    {
        _sheet.Cells.SetValue(0, 3, "d");
        _sheet.SetFormat(new Region(0, 9), new CellFormat { BackgroundColor = "red" });
        var before = Snapshot(_sheet);

        Move("D1", "B1", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetValue(0, 1).Should().Be("d");
        _sheet.GetFormat(0, 9).BackgroundColor.Should().Be("red");
        _sheet.Commands.Undo();
        Snapshot(_sheet).Should().Be(before);
        _sheet.Commands.Redo();
        _sheet.GetFormat(0, 9).BackgroundColor.Should().Be("red");
    }

    [Test]
    public void Spill_Blocked_By_The_Edge_Stays_Blocked()
    {
        _sheet.Cells.SetValue(0, 3, "d");
        _sheet.Cells.SetFormula(1, 7, "={1,2,3,4}"); // H2 would spill past J2
        var spill = _sheet.Cells.GetValue(1, 7);

        Move("D1", "B1", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetValue(1, 7).Should().Be(spill);
        _sheet.Cells.GetValue(1, 8).Should().BeNull();
        _sheet.Cells.GetValue(1, 10).Should().BeNull(); // past the edge

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(1, 7).Should().Be(spill);
    }

    [Test]
    public void Validators_Metadata_And_Conditional_Formats_Move_With_The_Block()
    {
        var validator = new NumberValidator(true);
        _sheet.Cells.SetValue(0, 1, 7);
        _sheet.Cells.SetValue(0, 2, "c");
        _sheet.Validators.Add(new Region(0, 1), validator);
        _sheet.Cells.SetCellMetaData(0, 1, "m", "x");
        _sheet.ConditionalFormats.Apply(new Region(0, 1),
            new ConditionalFormat((_, _) => true, _ => new CellFormat { BackgroundColor = "green" }));

        Move("B1", "D1", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetValue(0, 2).Should().Be(7);
        _sheet.Validators.Get(0, 2).Should().Equal(validator);
        _sheet.Validators.Get(0, 1).Should().BeEmpty();
        _sheet.Cells.GetMetaData(0, 2, "m").Should().Be("x");
        _sheet.Cells.GetMetaData(0, 1, "m").Should().BeNull();
        _sheet.ConditionalFormats.GetFormatResult(0, 2)!.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(0, 1).Should().BeNull();

        _sheet.Commands.Undo();

        _sheet.Validators.Get(0, 1).Should().Equal(validator);
        _sheet.Validators.Get(0, 2).Should().BeEmpty();
        _sheet.Cells.GetMetaData(0, 1, "m").Should().Be("x");
        _sheet.Cells.GetMetaData(0, 2, "m").Should().BeNull();
        _sheet.ConditionalFormats.GetFormatResult(0, 1)!.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(0, 2).Should().BeNull();

        _sheet.Commands.Redo();
        _sheet.Validators.Get(0, 2).Should().Equal(validator);
    }
}
