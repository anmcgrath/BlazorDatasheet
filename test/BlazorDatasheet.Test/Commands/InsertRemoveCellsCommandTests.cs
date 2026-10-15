using System.Linq;
using System.Text;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class InsertRemoveCellsCommandTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet("Sheet1", 10, 10);
    }

    private bool Insert(string region, Axis axis) => _sheet.InsertCells(_sheet.Range(region)!.Region, axis);

    private bool Remove(string region, Axis axis) => _sheet.RemoveCells(_sheet.Range(region)!.Region, axis);

    private void SetRowValues(int row, params object[] values)
    {
        for (var c = 0; c < values.Length; c++)
            _sheet.Cells.SetValue(row, c, values[c]);
    }

    private object?[] RowValues(int row, int count) =>
        Enumerable.Range(0, count).Select(c => _sheet.Cells.GetValue(row, c)).ToArray();

    private object?[] ColValues(int col, int count) =>
        Enumerable.Range(0, count).Select(r => _sheet.Cells.GetValue(r, col)).ToArray();

    /// <summary>
    /// Everything visible about the cells of the sheet, for comparing states.
    /// </summary>
    private string Snapshot()
    {
        var sb = new StringBuilder();
        for (var r = 0; r < _sheet.NumRows; r++)
        for (var c = 0; c < _sheet.NumCols; c++)
        {
            sb.Append(r).Append(',').Append(c).Append(':')
                .Append(_sheet.Cells.GetValue(r, c)).Append('|')
                .Append(_sheet.Cells.GetFormulaString(r, c)).Append('|')
                .Append(_sheet.GetFormat(r, c).BackgroundColor).Append('|')
                .Append(_sheet.Cells.GetCellType(r, c)).Append('|')
                .Append(_sheet.Cells.GetMerge(r, c)).Append('|')
                .Append(_sheet.Validators.Get(r, c).Count()).Append('|')
                .Append(_sheet.Cells.GetMetaData(r, c, "m")).Append('|')
                .Append(_sheet.ConditionalFormats.GetFormatResult(r, c)?.BackgroundColor)
                .AppendLine();
        }

        return sb.ToString();
    }

    [Test]
    public void Insert_Shift_Right_Moves_Values_In_Band_Only()
    {
        SetRowValues(0, "a", "b", "c", "d");
        SetRowValues(1, "e", "f", "g", "h");
        SetRowValues(2, "i", "j", "k", "l");

        Insert("B2", Axis.Col).Should().BeTrue();

        RowValues(0, 5).Should().Equal(new object?[] { "a", "b", "c", "d", null });
        RowValues(1, 5).Should().Equal(new object?[] { "e", null, "f", "g", "h" });
        RowValues(2, 5).Should().Equal(new object?[] { "i", "j", "k", "l", null });
    }

    [Test]
    public void Insert_Shift_Down_Moves_Values_In_Band_Only()
    {
        for (var r = 0; r < 4; r++)
        {
            _sheet.Cells.SetValue(r, 0, $"a{r}");
            _sheet.Cells.SetValue(r, 1, $"b{r}");
        }

        Insert("B2:B3", Axis.Row).Should().BeTrue();

        ColValues(0, 6).Should().Equal(new object?[] { "a0", "a1", "a2", "a3", null, null });
        ColValues(1, 6).Should().Equal(new object?[] { "b0", null, null, "b1", "b2", "b3" });
    }

    [Test]
    public void Remove_Shift_Left_And_Up_Closes_The_Gap_In_Band_Only()
    {
        SetRowValues(0, "a", "b", "c", "d");
        SetRowValues(1, "e", "f", "g", "h");

        Remove("B2:C2", Axis.Col).Should().BeTrue();
        RowValues(0, 4).Should().Equal(new object?[] { "a", "b", "c", "d" });
        RowValues(1, 4).Should().Equal(new object?[] { "e", "h", null, null });

        Remove("A1", Axis.Row).Should().BeTrue();
        ColValues(0, 3).Should().Equal(new object?[] { "e", null, null });
        ColValues(1, 2).Should().Equal(new object?[] { "b", "h" });
    }

    [Test]
    public void Formats_Types_Merges_Validators_Metadata_And_Conditional_Formats_Move_With_The_Cells()
    {
        _sheet.SetFormat(new Region(1, 1), new CellFormat { BackgroundColor = "red" });
        _sheet.SetFormat(new Region(0, 1), new CellFormat { BackgroundColor = "blue" });
        _sheet.Cells.SetType(new Region(1, 2), "boolean");
        _sheet.Cells.Merge(new Region(1, 1, 3, 4)); // D2:E2
        _sheet.Validators.Add(new Region(1, 1), new NumberValidator(true));
        _sheet.Cells.SetCellMetaData(1, 2, "m", "meta");
        _sheet.ConditionalFormats.Apply(new Region(1, 5),
            new ConditionalFormat((_, _) => true, _ => new CellFormat { BackgroundColor = "green" }));

        Insert("B2", Axis.Col).Should().BeTrue();

        _sheet.GetFormat(1, 1).BackgroundColor.Should().BeNull();
        _sheet.GetFormat(1, 2).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(0, 1).BackgroundColor.Should().Be("blue");
        _sheet.Cells.GetCellType(1, 3).Should().Be("boolean");
        _sheet.Cells.GetCellType(1, 2).Should().Be("default");
        _sheet.Cells.GetMerge(1, 4).Should().BeEquivalentTo(new Region(1, 1, 4, 5));
        _sheet.Validators.Get(1, 1).Should().BeEmpty();
        _sheet.Validators.Get(1, 2).Should().HaveCount(1);
        _sheet.Cells.GetMetaData(1, 3, "m").Should().Be("meta");
        _sheet.Cells.GetMetaData(1, 2, "m").Should().BeNull();
        _sheet.ConditionalFormats.GetFormatResult(1, 6)?.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(1, 5).Should().BeNull();

        _sheet.Commands.Undo();
        _sheet.GetFormat(1, 1).BackgroundColor.Should().Be("red");
        _sheet.Cells.GetCellType(1, 2).Should().Be("boolean");
        _sheet.Cells.GetMerge(1, 3).Should().BeEquivalentTo(new Region(1, 1, 3, 4));
        _sheet.Validators.Get(1, 1).Should().HaveCount(1);
        _sheet.Cells.GetMetaData(1, 2, "m").Should().Be("meta");
        _sheet.ConditionalFormats.GetFormatResult(1, 5)?.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(1, 6).Should().BeNull();
    }

    [Test]
    public void Format_Straddling_The_Band_Is_Split()
    {
        _sheet.SetFormat(new Region(0, 2, 1, 1), new CellFormat { BackgroundColor = "red" }); // B1:B3

        Insert("A2", Axis.Col).Should().BeTrue();

        _sheet.GetFormat(0, 1).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(1, 1).BackgroundColor.Should().BeNull();
        _sheet.GetFormat(1, 2).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(2, 1).BackgroundColor.Should().Be("red");
    }

    [Test]
    public void Remove_Shifts_Up_Formats_And_Validators()
    {
        _sheet.SetFormat(new Region(3, 0), new CellFormat { BackgroundColor = "red" });
        _sheet.Validators.Add(new Region(3, 0), new NumberValidator(true));

        Remove("A1:A2", Axis.Row).Should().BeTrue();

        _sheet.GetFormat(1, 0).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(3, 0).BackgroundColor.Should().BeNull();
        _sheet.Validators.Get(1, 0).Should().HaveCount(1);
        _sheet.Validators.Get(3, 0).Should().BeEmpty();
    }

    [Test]
    public void Insert_Refuses_A_Merge_Straddling_The_Band()
    {
        _sheet.Cells.Merge(new Region(1, 2, 3, 3)); // D2:D3
        _sheet.Cells.SetValue(1, 1, "x");
        var undoCount = _sheet.Commands.GetUndoCommands().Count();

        Insert("B2", Axis.Col).Should().BeFalse();
        _sheet.Cells.GetValue(1, 1).Should().Be("x");
        _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount);
    }

    [Test]
    public void Remove_Refuses_A_Partially_Removed_Merge()
    {
        _sheet.Cells.Merge(new Region(1, 1, 1, 2)); // B2:C2

        Remove("C2", Axis.Col).Should().BeFalse();
        Remove("B2:C2", Axis.Col).Should().BeTrue();
        _sheet.Cells.GetMerge(1, 1).Should().BeNull();
    }

    [Test]
    public void Insert_Refuses_To_Push_A_Value_Or_Formula_Off_The_Edge()
    {
        _sheet.Cells.SetValue(1, 9, "edge");
        Insert("B2", Axis.Col).Should().BeFalse();
        _sheet.Cells.GetValue(1, 9).Should().Be("edge");

        _sheet.Cells.ClearCells(new Region(1, 9));
        _sheet.Cells.SetFormula(9, 1, "=1");
        Insert("B2", Axis.Row).Should().BeFalse();

        // a value in the last column outside the band is not pushed anywhere
        Insert("B3", Axis.Col).Should().BeTrue();
    }

    [Test]
    public void Formats_Pushed_Off_The_Edge_Are_Dropped_And_Restored_On_Undo()
    {
        _sheet.SetFormat(new Region(1, 9), new CellFormat { BackgroundColor = "red" });
        _sheet.SetFormat(new Region(1, 8), new CellFormat { BackgroundColor = "blue" });
        _sheet.Cells.SetType(new Region(1, 9), "boolean");

        Insert("B2", Axis.Col).Should().BeTrue();
        _sheet.GetFormat(1, 9).BackgroundColor.Should().Be("blue");
        _sheet.Cells.GetCellType(1, 9).Should().Be("default");

        _sheet.Commands.Undo();
        _sheet.GetFormat(1, 9).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(1, 8).BackgroundColor.Should().Be("blue");
        _sheet.Cells.GetCellType(1, 9).Should().Be("boolean");

        _sheet.Commands.Redo();
        _sheet.GetFormat(1, 9).BackgroundColor.Should().Be("blue");
    }

    [Test]
    public void Refuses_Read_Only_Cells_In_The_Shifted_Cells()
    {
        _sheet.SetFormat(new Region(1, 5), new CellFormat { IsReadOnly = true });
        Insert("B2", Axis.Col).Should().BeFalse();
        Remove("B2", Axis.Col).Should().BeFalse();
        Insert("B3", Axis.Col).Should().BeTrue();
    }

    [Test]
    public void Refuses_Locked_Cells_When_Protected()
    {
        _sheet.SetFormat(new Region(0, 0, 0, 9), new CellFormat { IsLocked = false });
        _sheet.Protection.Protect(new SheetProtectionOptions { AllowFormatCells = true });

        Insert("B2", Axis.Col).Should().BeFalse();
        Remove("B2", Axis.Col).Should().BeFalse();
        Insert("B1", Axis.Col).Should().BeTrue();

        _sheet.Protection.Unprotect();
        _sheet.Protection.Protect();
        Insert("B1", Axis.Col).Should().BeFalse();
    }

    [Test]
    public void Formula_Outside_The_Band_With_A_Spanning_Range_Recalculates()
    {
        _sheet.Cells.SetValue(1, 2, 5); // C2
        _sheet.Cells.SetFormula(8, 0, "=SUM(A1:C5)");
        _sheet.Cells.GetValue(8, 0).Should().Be(5);

        Insert("B2", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetFormulaString(8, 0).Should().Be("=SUM(A1:C5)");
        _sheet.Cells.GetValue(8, 0).Should().Be(0);

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(8, 0).Should().Be(5);
    }

    [Test]
    public void Formula_Outside_The_Band_Referencing_A_Moved_Cell_Follows_It()
    {
        _sheet.Cells.SetValue(1, 1, 3); // B2
        _sheet.Cells.SetFormula(8, 0, "=B2*2");

        Insert("A2", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetFormulaString(8, 0).Should().Be("=C2*2");
        _sheet.Cells.GetValue(8, 0).Should().Be(6);
        _sheet.Cells.SetValue(1, 2, 4);
        _sheet.Cells.GetValue(8, 0).Should().Be(8);

        _sheet.Commands.Undo();
        _sheet.Commands.Undo();
        _sheet.Cells.GetFormulaString(8, 0).Should().Be("=B2*2");
        _sheet.Cells.GetValue(8, 0).Should().Be(6);
    }

    [Test]
    public void Formula_Inside_The_Band_Is_Adjusted_And_Moves()
    {
        _sheet.Cells.SetValue(1, 1, 2); // B2
        _sheet.Cells.SetFormula(1, 3, "=B2+1"); // D2

        Insert("A2", Axis.Col).Should().BeTrue();

        _sheet.Cells.HasFormula(1, 3).Should().BeFalse();
        _sheet.Cells.GetFormulaString(1, 4).Should().Be("=C2+1");
        _sheet.Cells.GetValue(1, 4).Should().Be(3);
        _sheet.Cells.SetValue(1, 2, 10);
        _sheet.Cells.GetValue(1, 4).Should().Be(11);
    }

    [Test]
    public void Reference_To_A_Removed_Cell_Becomes_Ref_Error()
    {
        _sheet.Cells.SetValue(1, 1, 2); // B2
        _sheet.Cells.SetValue(1, 2, 7); // C2
        _sheet.Cells.SetFormula(5, 0, "=B2");
        _sheet.Cells.SetFormula(6, 0, "=C2");

        Remove("B2", Axis.Col).Should().BeTrue();

        _sheet.Cells.GetFormulaString(5, 0).Should().Contain("#REF!");
        _sheet.Cells.GetFormulaString(6, 0).Should().Be("=B2");
        _sheet.Cells.GetValue(6, 0).Should().Be(7);

        _sheet.Commands.Undo();
        _sheet.Cells.GetFormulaString(5, 0).Should().Be("=B2");
        _sheet.Cells.GetValue(5, 0).Should().Be(2);
        _sheet.Cells.GetFormulaString(6, 0).Should().Be("=C2");
    }

    [Test]
    public void Spill_Crossing_The_Band_Is_Wiped_And_Respilled_And_Undo_Restores()
    {
        _sheet.Cells.SetFormula(1, 3, "={1,2}"); // D2 spills D2:E2
        _sheet.Cells.SetFormula(0, 6, "={1;2;3}"); // G1 spills G1:G3, crosses row 2
        RowValues(1, 8).Should().Equal(new object?[] { null, null, null, 1, 2, null, 2, null });

        Insert("B2", Axis.Col).Should().BeTrue();

        RowValues(1, 8).Should().Equal(new object?[] { null, null, null, null, 1, 2, 2, null });
        _sheet.Cells.GetSpillRegionForAnchor(1, 4).Should().BeEquivalentTo(new Region(1, 1, 4, 5));
        ColValues(6, 3).Should().Equal(new object?[] { 1, 2, 3 });
        _sheet.Cells.GetSpillRegionForAnchor(0, 6).Should().BeEquivalentTo(new Region(0, 2, 6, 6));

        _sheet.Commands.Undo();

        RowValues(1, 8).Should().Equal(new object?[] { null, null, null, 1, 2, null, 2, null });
        _sheet.Cells.GetSpillRegionForAnchor(1, 3).Should().BeEquivalentTo(new Region(1, 1, 3, 4));
        _sheet.Cells.GetSpillRegionForAnchor(0, 6).Should().BeEquivalentTo(new Region(0, 2, 6, 6));
        _sheet.Cells.IsSpillAnchor(1, 4).Should().BeFalse();
    }

    [Test]
    public void Named_Range_Inside_The_Band_Shifts_And_Straddling_Stays()
    {
        _sheet.NamedRanges.Set("inside", "C2");
        _sheet.NamedRanges.Set("straddle", "C1:C3");
        _sheet.Cells.SetValue(1, 2, 4);
        _sheet.Cells.SetFormula(8, 0, "=inside");

        Insert("A2", Axis.Col).Should().BeTrue();

        _sheet.NamedRanges.GetRangeString("inside").Should().Contain("D").And.Contain("2");
        _sheet.NamedRanges.GetRangeString("straddle").Should().Contain("C1").And.Contain("C3");
        _sheet.Cells.GetValue(8, 0).Should().Be(4);

        _sheet.Commands.Undo();
        _sheet.NamedRanges.GetRangeString("inside").Should().Contain("C").And.Contain("2");
    }

    [Test]
    public void Tracked_Ranges_Inside_Shift_Straddling_Stay_And_Removed_Are_Deleted()
    {
        _workbook.TrackedRanges.Track("inside", _sheet.Name, new Region(1, 2));
        _workbook.TrackedRanges.Track("straddle", _sheet.Name, new Region(0, 2, 2, 2));
        _workbook.TrackedRanges.Track("removed", _sheet.Name, new Region(1, 1));

        Remove("B2", Axis.Col).Should().BeTrue();

        _workbook.TrackedRanges.Get("inside")!.Region.Should().BeEquivalentTo(new Region(1, 1));
        _workbook.TrackedRanges.Get("straddle")!.Region.Should().BeEquivalentTo(new Region(0, 2, 2, 2));
        _workbook.TrackedRanges.Get("removed")!.IsDeleted.Should().BeTrue();

        _sheet.Commands.Undo();
        _workbook.TrackedRanges.Get("inside")!.Region.Should().BeEquivalentTo(new Region(1, 2));
        _workbook.TrackedRanges.Get("removed")!.IsDeleted.Should().BeFalse();
        _workbook.TrackedRanges.Get("removed")!.Region.Should().BeEquivalentTo(new Region(1, 1));

        Insert("A2", Axis.Col).Should().BeTrue();
        _workbook.TrackedRanges.Get("inside")!.Region.Should().BeEquivalentTo(new Region(1, 3));
        _workbook.TrackedRanges.Get("straddle")!.Region.Should().BeEquivalentTo(new Region(0, 2, 2, 2));
    }

    [Test]
    public void Conditional_Format_Formula_Referencing_Inside_The_Band_Is_Adjusted()
    {
        _sheet.Cells.SetValue(1, 2, 5); // C2
        _sheet.ConditionalFormats.Apply(new Region(5, 0),
            new FormulaConditionalFormat("=C2>1", new CellFormat { BackgroundColor = "green" }));
        _sheet.ConditionalFormats.GetFormatResult(5, 0)?.BackgroundColor.Should().Be("green");

        Insert("A2", Axis.Col).Should().BeTrue();

        var cf = (FormulaConditionalFormat)_sheet.ConditionalFormats.GetAllFormats().Single().Data;
        cf.Formula.Should().Be("=D2>1");
        _sheet.ConditionalFormats.GetFormatResult(5, 0)?.BackgroundColor.Should().Be("green");

        _sheet.Commands.Undo();
        cf.Formula.Should().Be("=C2>1");
    }

    [Test]
    public void Conditional_Format_Relative_Formula_Keeps_Evaluating_The_Same_Cells_When_Its_Anchor_Is_Removed()
    {
        // =A1>1 applied to A1:C1: each cell tests itself
        _sheet.ConditionalFormats.Apply(new Region(0, 0, 0, 2),
            new FormulaConditionalFormat("=A1>1", new CellFormat { BackgroundColor = "green" }));
        SetRowValues(0, 0, 5, 0);

        Remove("A1", Axis.Col).Should().BeTrue();

        // B1 (5) is now A1 and C1 (0) is now B1
        _sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(0, 1).Should().BeNull();
        _sheet.ConditionalFormats.GetFormatResult(0, 2).Should().BeNull();
    }

    [Test]
    public void Undo_Then_Redo_Twice_Gives_Identical_State_And_One_History_Entry()
    {
        SetRowValues(1, 1, 2, 3, 4);
        _sheet.Cells.SetFormula(5, 0, "=SUM(B2:D2)");
        _sheet.Cells.SetFormula(1, 5, "=A2+B2");
        _sheet.SetFormat(new Region(1, 2), new CellFormat { BackgroundColor = "red" });
        _sheet.Cells.Merge(new Region(1, 1, 6, 7));
        _sheet.Cells.SetType(new Region(1, 3), "boolean");
        _sheet.Validators.Add(new Region(1, 2), new NumberValidator(true));
        _sheet.Cells.SetCellMetaData(1, 3, "m", "x");

        foreach (var axis in new[] { Axis.Col, Axis.Row })
        {
            foreach (var remove in new[] { false, true })
            {
                var before = Snapshot();
                var undoCount = _sheet.Commands.GetUndoCommands().Count();

                (remove ? Remove("B2:C2", axis) : Insert("B2:C2", axis)).Should().BeTrue();
                _sheet.Commands.GetUndoCommands().Count().Should().Be(undoCount + 1);
                var after = Snapshot();
                after.Should().NotBe(before);

                for (var i = 0; i < 2; i++)
                {
                    _sheet.Commands.Undo().Should().BeTrue();
                    Snapshot().Should().Be(before);
                    _sheet.Commands.Redo().Should().BeTrue();
                    Snapshot().Should().Be(after);
                }

                _sheet.Commands.Undo();
                Snapshot().Should().Be(before);
            }
        }
    }

    [Test]
    public void Formula_On_Another_Sheet_Is_Updated()
    {
        var other = _workbook.AddSheet("Sheet2", 10, 10);
        _sheet.Cells.SetValue(1, 1, 3); // B2
        other.Cells.SetFormula(0, 0, "=Sheet1!B2");
        other.Cells.SetFormula(1, 0, "=SUM(Sheet1!A1:C5)");

        Insert("A2:B2", Axis.Col).Should().BeTrue();

        other.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!D2");
        other.Cells.GetValue(0, 0).Should().Be(3);
        other.Cells.GetFormulaString(1, 0).Should().Be("=SUM(Sheet1!A1:C5)");
        other.Cells.GetValue(1, 0).Should().Be(0);

        _sheet.Commands.Undo();
        other.Cells.GetFormulaString(0, 0).Should().Be("=Sheet1!B2");
        other.Cells.GetValue(1, 0).Should().Be(3);
    }

    [Test]
    public void Region_Outside_The_Sheet_Is_Refused()
    {
        _sheet.InsertCells(new Region(20, 20), Axis.Col).Should().BeFalse();
        _sheet.RemoveCells(new Region(20, 20), Axis.Row).Should().BeFalse();
    }
}
