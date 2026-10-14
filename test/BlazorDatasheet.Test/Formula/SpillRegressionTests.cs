using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class SpillRegressionTests
{
    private static ErrorType? ErrorAt(Sheet sheet, int row, int col) =>
        sheet.Cells.GetCellValue(row, col).Data is FormulaError error ? error.ErrorType : null;

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    public void Shrinking_A_Spill_Unblocks_An_Overlapping_Array_Formula(int size, int blockedRow)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 3, 4);
        sheet.Cells.SetFormula(0, 1, "=SEQUENCE(D1)");
        sheet.Cells.SetFormula(blockedRow, 0, "={4,5}");
        ErrorAt(sheet, blockedRow, 0).Should().Be(ErrorType.Spill);

        sheet.Cells.SetValue(0, 3, size);

        sheet.Cells.GetValue(blockedRow, 0).Should().Be(4);
        sheet.Cells.GetValue(blockedRow, 1).Should().Be(5);
        sheet.Cells.IsSpilledCell(blockedRow, 1).Should().BeTrue();

        // Once the competing spill owns the released cell, growing the original must respect it.
        sheet.Cells.SetValue(0, 3, 4);
        ErrorAt(sheet, 0, 1).Should().Be(ErrorType.Spill);
        sheet.Cells.GetValue(blockedRow, 1).Should().Be(5);
        sheet.Cells.SetValue(0, 3, size);
        sheet.Cells.GetValue(0, 1).Should().Be(1);
        sheet.Cells.GetValue(blockedRow, 1).Should().Be(5);
    }

    [Test]
    public void Unchanged_Spill_Footprints_Do_Not_Repeatedly_Retry_Blocked_Formulas()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 3, 1);
        sheet.Cells.SetFormula(0, 1, "=SEQUENCE(3,1,D1)");
        sheet.Cells.SetFormula(1, 0, "={4,5}");
        var evaluations = 0;
        sheet.FormulaEngine.CalculationCompleted += (_, e) => evaluations += e.FormulaCount;

        sheet.Cells.SetValue(0, 3, 2);

        sheet.Cells.GetValue(1, 1).Should().Be(3);
        ErrorAt(sheet, 1, 0).Should().Be(ErrorType.Spill);
        evaluations.Should().BeLessThan(5);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Merging_A_Spill_Blocks_It_And_Unmerging_Rebuilds_It(bool hidesDescendant)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "={1;2;3}");
        var merge = hidesDescendant ? new Region(1, 2, 0, 0) : new Region(1, 1, 0, 1);

        sheet.Cells.Merge(merge);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Spill);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
        sheet.Cells.GetValue(2, 0).Should().BeNull();

        sheet.Cells.UnMerge([merge]);
        sheet.Cells.GetValue(1, 0).Should().Be(2);
        sheet.Cells.IsSpilledCell(2, 0).Should().BeTrue();

        sheet.Commands.Undo();
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Spill);
        sheet.Commands.Redo();
        sheet.Cells.GetValue(2, 0).Should().Be(3);
        sheet.Commands.Undo();
        sheet.Commands.Undo();
        sheet.Cells.GetValue(2, 0).Should().Be(3);
        sheet.Cells.GetMerge(1, 0).Should().BeNull();
    }

    [TestCase("=SEQUENCE(2,1,A2+1)")]
    [TestCase("=SEQUENCE(2,1,-A2)")]
    [TestCase("=SEQUENCE(3,1,SUM(A2:A3)+1)")]
    public void Reading_Own_Spill_Is_Circular_And_Does_Not_Write_Descendants(string formula)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, formula);

        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
        sheet.Cells.GetSpillRegionForAnchor(0, 0).Should().BeNull();
        sheet.FormulaEngine.CalculateSheet(true);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
    }

    [Test]
    public void Reading_Own_Spill_Through_Another_Formula_Is_Circular()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 1, "=A2+1");
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(2,1,B1+1)");

        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
        sheet.FormulaEngine.CalculateSheet(true);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);

        sheet.Cells.SetValue(0, 1, 5);
        sheet.Cells.GetValue(0, 0).Should().Be(6);
        sheet.Cells.GetValue(1, 0).Should().Be(7);
    }

    [Test]
    public void Reading_Own_Spill_Through_A_Named_Formula_Is_Circular()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Input", 10, 10);
        sheet.FormulaEngine.SetVariable("spillInput", "=Input!A2+1");
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(2,1,spillInput)");

        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
    }

    [Test]
    public void Growing_Into_A_Referenced_Cell_Detects_A_Cycle_And_Shrinking_Recovers()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 3, 2);
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(D1,1,A3+1)");
        sheet.Cells.GetValue(1, 0).Should().Be(2);

        sheet.Cells.SetValue(0, 3, 3);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
        sheet.Cells.GetValue(2, 0).Should().BeNull();

        sheet.Cells.SetValue(0, 3, 2);
        sheet.Cells.GetValue(0, 0).Should().Be(1);
        sheet.Cells.GetValue(1, 0).Should().Be(2);
    }

    [Test]
    public void Spill_Cycle_Detection_Distinguishes_Sheets()
    {
        var workbook = new Workbook();
        var source = workbook.AddSheet("Input", 10, 10);
        var target = workbook.AddSheet("Output", 10, 10);
        source.Cells.SetValue(1, 0, 5);
        target.Cells.SetFormula(0, 0, "=SEQUENCE(2,1,Input!A2+1)");

        target.Cells.GetValue(0, 0).Should().Be(6);
        target.Cells.GetValue(1, 0).Should().Be(7);
    }

    [TestCase("=IF(TRUE,{1;2},A2)")]
    [TestCase("=IF(TRUE,{1;2},SUM(A2:A3))")]
    public void Unused_Conditional_References_Do_Not_Make_A_Spill_Circular(string formula)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, formula);

        sheet.Cells.GetValue(0, 0).Should().Be(1);
        sheet.Cells.GetValue(1, 0).Should().Be(2);
    }

    [Test]
    public void Unused_Indirect_Conditional_Reference_Does_Not_Make_A_Spill_Circular()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 1, "=IF(TRUE,5,A2)");
        var evaluations = 0;
        sheet.FormulaEngine.CalculationCompleted += (_, e) => evaluations += e.FormulaCount;
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(2,1,SUM(B1:B2))");

        sheet.Cells.GetValue(0, 0).Should().Be(5);
        sheet.Cells.GetValue(1, 0).Should().Be(6);
        evaluations.Should().BeLessThan(10, "a stable conditional feedback path should converge");
    }

    [Test]
    public void Reading_Own_Spill_Through_A_Range_Of_Formulas_Is_Circular()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 1, "=A2+1");
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(2,1,SUM(B1:B2))");

        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Circular);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
    }
    [Test]
    public void Error_Valued_Spill_Recalculates_And_Recovers()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 3, 0);
        sheet.Cells.SetFormula(0, 0, "={1;2}/D1");
        sheet.Cells.SetFormula(0, 1, "=SUM(A1#)");

        sheet.FormulaEngine.CalculateSheet(true);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Div0);
        ErrorAt(sheet, 1, 0).Should().Be(ErrorType.Div0);
        ErrorAt(sheet, 0, 1).Should().Be(ErrorType.Div0);

        sheet.Cells.SetValue(0, 3, 1);
        sheet.Cells.GetValue(0, 1).Should().Be(3);
        sheet.Cells.GetValue(1, 0).Should().Be(2);
    }

    [Test]
    public void Lifted_If_Stores_Values_And_Updates_When_The_Reference_Changes()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 1, 20);
        sheet.Cells.SetFormula(0, 0, "=IF({TRUE;FALSE},10,B1)");
        sheet.Cells.GetValue(1, 0).Should().Be(20);
        sheet.Cells.SetValue(0, 1, 30);
        sheet.Cells.GetValue(1, 0).Should().Be(30);
    }

    [TestCase("=SEQUENCE({2;3})")]
    [TestCase("=SORT({2;1},{1;1})")]
    public void Lifting_An_Array_Returning_Function_Returns_Calc_And_Retracts_The_Old_Spill(string formula)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(2)");
        sheet.Cells.SetFormula(0, 0, formula);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Calc);
        sheet.Cells.GetValue(1, 0).Should().BeNull();
        sheet.Cells.IsSpillAnchor(0, 0).Should().BeFalse();
        sheet.FormulaEngine.CalculateSheet(true);
        ErrorAt(sheet, 0, 0).Should().Be(ErrorType.Calc);
    }

    [Test]
    public void Array_Returning_Function_Accepts_A_Singleton_Scalar_Argument()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE({2})");
        sheet.Cells.GetValue(1, 0).Should().Be(2);
    }

    [Test]
    public void Same_Sized_Spill_Updates_Values_Without_Emitting_Ownership_Changes()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 3, 1);
        sheet.Cells.SetFormula(0, 0, "=SEQUENCE(2,1,D1)");
        sheet.Cells.SetFormula(0, 1, "=A2");
        var spillChanges = 0;
        var descendantDirtied = false;
        sheet.Cells.SpillChanged += (_, _) => spillChanges++;
        sheet.SheetDirty += (_, e) => descendantDirtied |= e.DirtyRows.Covers(1, 1) &&
                                                        e.DirtyColStart <= 0 && e.DirtyColEnd >= 0;

        sheet.Cells.SetValue(0, 3, 5);
        sheet.Cells.GetValue(1, 0).Should().Be(6);
        sheet.Cells.GetValue(0, 1).Should().Be(6);
        spillChanges.Should().Be(0);
        descendantDirtied.Should().BeTrue();
    }
}
