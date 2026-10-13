using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class SpillIntegrationTests
{
    private Sheet _sheet;

    /// <summary>COUNTTO(n) gives the column 1..n; EMPTYARR() gives an array with no rows.</summary>
    private static readonly FormulaOptions Options = new()
    {
        ConfigureFunctions = builder =>
        {
            builder.Add(new FunctionDescriptor("COUNTTO",
                [new ParameterDefinition("n", ParameterType.Integer)],
                (args, _) => CellValue.Array(Enumerable.Range(1, (int)args[0].NumberValue)
                    .Select(i => new[] { CellValue.Number(i) }).ToArray()),
                returnShape: ReturnShape.Array));
            builder.Add(new FunctionDescriptor("EMPTYARR", [],
                (_, _) => CellValue.Array([]), returnShape: ReturnShape.Array));
        }
    };

    [SetUp]
    public void Setup()
    {
        _sheet = new Workbook(Options).AddSheet(10, 10);
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetValue(2, 0, 3);
    }

    private ErrorType? ErrorAt(int row, int col) =>
        _sheet.Cells.GetCellValue(row, col).Data is FormulaError e ? e.ErrorType : null;

    private object?[] Column(int col, int rows) =>
        Enumerable.Range(0, rows).Select(r => _sheet.Cells.GetValue(r, col)).ToArray();

    [Test]
    public void Range_Formula_Spills_Down_From_The_Anchor()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");

        Column(1, 4).Should().Equal(1, 2, 3, null);
        _sheet.Cells.GetSpillRegion(1, 1).Should().BeEquivalentTo(new Region(0, 2, 1, 1));
        _sheet.Cells.HasFormula(1, 1).Should().BeFalse();
        _sheet.Cells.IsSpilledCell(1, 1).Should().BeTrue();
        _sheet.Cells.IsSpillAnchor(0, 1).Should().BeTrue();
    }

    [Test]
    public void Array_Constant_Spills_Across_Rows_And_Columns()
    {
        _sheet.Cells.SetFormula(2, 2, "={1,2;3,4}");
        _sheet.Cells.GetValue(2, 2).Should().Be(1);
        _sheet.Cells.GetValue(2, 3).Should().Be(2);
        _sheet.Cells.GetValue(3, 2).Should().Be(3);
        _sheet.Cells.GetValue(3, 3).Should().Be(4);
    }

    [Test]
    public void Spill_Updates_When_Source_Values_Change()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3*2");
        _sheet.Cells.SetValue(1, 0, 10);
        Column(1, 3).Should().Equal(2, 20, 6);
    }

    [Test]
    public void Spill_Blocked_By_A_Value_Gives_Spill_Error_And_Leaves_Other_Cells()
    {
        _sheet.Cells.SetValue(2, 1, "x");
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");

        ErrorAt(0, 1).Should().Be(ErrorType.Spill);
        _sheet.Cells.GetValue(1, 1).Should().BeNull();
        _sheet.Cells.GetValue(2, 1).Should().Be("x");
        _sheet.Cells.IsSpilledCell(1, 1).Should().BeFalse();

        // removing the obstruction lets it spill
        _sheet.Cells.ClearCells(new Region(2, 1));
        Column(1, 3).Should().Equal(1, 2, 3);
    }

    [Test]
    public void Spill_Is_Blocked_By_A_Formula_A_Merge_Or_Another_Spill()
    {
        _sheet.Cells.SetFormula(1, 1, "=\"\"");
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        ErrorAt(0, 1).Should().Be(ErrorType.Spill);

        _sheet.Cells.Merge(new Region(1, 2, 2, 2));
        _sheet.Cells.SetFormula(0, 2, "=A1:A3");
        ErrorAt(0, 2).Should().Be(ErrorType.Spill);

        _sheet.Cells.SetFormula(1, 3, "=A1:A3");
        _sheet.Cells.SetFormula(0, 3, "=A1:A3");
        ErrorAt(0, 3).Should().Be(ErrorType.Spill);
        Column(3, 5).Select(v => v is FormulaError ? "err" : v).Should().Equal("err", 1, 2, 3, null);
    }

    [Test]
    public void Spill_Off_The_Sheet_Is_Blocked()
    {
        _sheet.Cells.SetFormula(8, 0, "={1;2;3}");
        ErrorAt(8, 0).Should().Be(ErrorType.Spill);
        _sheet.Cells.GetValue(9, 0).Should().BeNull();
    }

    [Test]
    public void Spill_Shrinks_And_Clears_The_Cells_It_Leaves()
    {
        _sheet.Cells.SetValue(0, 2, 3);
        _sheet.Cells.SetFormula(0, 1, "=COUNTTO(C1)");
        Column(1, 4).Should().Equal(1, 2, 3, null);

        _sheet.Cells.SetValue(0, 2, 2);
        Column(1, 4).Should().Equal(1, 2, null, null);
        _sheet.Cells.IsSpilledCell(2, 1).Should().BeFalse();

        _sheet.Cells.SetValue(0, 2, 1);
        Column(1, 4).Should().Equal(1, null, null, null);
        _sheet.Cells.IsSpillAnchor(0, 1).Should().BeFalse();
    }

    [Test]
    public void Empty_Array_Result_Is_Calc_Error()
    {
        _sheet.Cells.SetFormula(0, 1, "=EMPTYARR()");
        ErrorAt(0, 1).Should().Be(ErrorType.Calc);
    }

    [Test]
    public void Undo_Of_The_Anchor_Formula_Clears_The_Spill_And_Redo_Restores_It()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Commands.Undo();
        Column(1, 3).Should().Equal(new object?[] { null, null, null });
        _sheet.Cells.GetSpillRegion(1, 1).Should().BeNull();

        _sheet.Commands.Redo();
        Column(1, 3).Should().Equal(1, 2, 3);
        _sheet.Cells.GetSpillRegion(1, 1).Should().NotBeNull();
    }

    [Test]
    public void Replacing_The_Anchor_Formula_Replaces_The_Spill()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Cells.SetFormula(0, 1, "=A1:A2*10");
        Column(1, 3).Should().Equal(10, 20, null);
        _sheet.Cells.SetValue(0, 1, "plain");
        Column(1, 3).Should().Equal(new object?[] { "plain", null, null });
        _sheet.Cells.GetSpillStore().Any().Should().BeFalse();
    }

    [Test]
    public void Value_Written_Into_A_Spilled_Cell_Blocks_The_Anchor_Until_Cleared()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Cells.SetValue(2, 1, "x");

        ErrorAt(0, 1).Should().Be(ErrorType.Spill);
        _sheet.Cells.GetValue(1, 1).Should().BeNull();
        _sheet.Cells.GetValue(2, 1).Should().Be("x");

        _sheet.Commands.Undo();
        Column(1, 3).Should().Equal(1, 2, 3);

        _sheet.Commands.Redo();
        ErrorAt(0, 1).Should().Be(ErrorType.Spill);
        _sheet.Cells.ClearCells(new Region(2, 1));
        Column(1, 3).Should().Equal(1, 2, 3);
    }

    [Test]
    public void Clearing_The_Anchor_Clears_The_Whole_Spill()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Cells.ClearCells(new Region(0, 1));
        Column(1, 3).Should().Equal(new object?[] { null, null, null });
        _sheet.Cells.GetSpillStore().Any().Should().BeFalse();

        _sheet.Commands.Undo();
        Column(1, 3).Should().Equal(1, 2, 3);
    }

    [Test]
    public void Clearing_A_Spilled_Cell_Leaves_The_Spill_In_Place()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Cells.ClearCells(new Region(1, 1));
        Column(1, 3).Should().Equal(1, 2, 3);
    }

    [Test]
    public void Inserting_A_Row_Above_Moves_The_Spill()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Rows.InsertAt(0);
        Column(1, 5).Should().Equal(null, 1, 2, 3, null);
        _sheet.Cells.GetSpillRegionForAnchor(1, 1).Should().BeEquivalentTo(new Region(1, 3, 1, 1));

        _sheet.Commands.Undo();
        Column(1, 4).Should().Equal(1, 2, 3, null);
        _sheet.Cells.GetSpillRegionForAnchor(0, 1).Should().BeEquivalentTo(new Region(0, 2, 1, 1));
    }

    [Test]
    public void Inserting_A_Row_Inside_The_Spill_Rebuilds_It_Below_The_Anchor()
    {
        _sheet.Cells.SetFormula(0, 1, "={1;2;3}");
        _sheet.Rows.InsertAt(1);
        Column(1, 5).Should().Equal(1, 2, 3, null, null);
        _sheet.Cells.GetSpillRegionForAnchor(0, 1).Should().BeEquivalentTo(new Region(0, 2, 1, 1));
    }

    [Test]
    public void Removing_The_Anchor_Row_Removes_The_Spill()
    {
        _sheet.Cells.SetFormula(0, 1, "={1;2;3}");
        _sheet.Rows.RemoveAt(0);
        Column(1, 3).Should().Equal(new object?[] { null, null, null });
        _sheet.Cells.GetSpillStore().Any().Should().BeFalse();

        _sheet.Commands.Undo();
        Column(1, 4).Should().Equal(1, 2, 3, null);
    }

    [Test]
    public void Formula_Reading_Spilled_Cells_Updates_With_The_Spill()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3*2");
        _sheet.Cells.SetFormula(0, 2, "=SUM(B2:B3)"); // does not include the anchor
        _sheet.Cells.GetValue(0, 2).Should().Be(10);

        _sheet.Cells.SetValue(2, 0, 10);
        _sheet.Cells.GetValue(0, 2).Should().Be(24);

        _sheet.FormulaEngine.CalculateSheet(true);
        _sheet.Cells.GetValue(0, 2).Should().Be(24);
    }

    [Test]
    public void Formula_Reading_Spilled_Cells_Sees_Them_Empty_When_The_Spill_Shrinks()
    {
        _sheet.Cells.SetValue(0, 3, 3);
        _sheet.Cells.SetFormula(0, 1, "=COUNTTO(D1)");
        _sheet.Cells.SetFormula(0, 2, "=SUM(B2:B3)");
        _sheet.Cells.GetValue(0, 2).Should().Be(5);

        _sheet.Cells.SetValue(0, 3, 2);
        _sheet.Cells.GetValue(0, 2).Should().Be(2);
    }

    [Test]
    public void Spill_Range_Reference_Follows_The_Spill_Size()
    {
        _sheet.Cells.SetValue(0, 3, 3);
        _sheet.Cells.SetFormula(0, 1, "=COUNTTO(D1)");
        _sheet.Cells.SetFormula(0, 2, "=B1#*10");
        Column(2, 4).Should().Equal(10, 20, 30, null);
        _sheet.Cells.SetFormula(0, 4, "=SUM(B1#)");
        _sheet.Cells.GetValue(0, 4).Should().Be(6);

        _sheet.Cells.SetValue(0, 3, 2);
        Column(2, 4).Should().Equal(10, 20, null, null);
        _sheet.Cells.GetValue(0, 4).Should().Be(3);

        _sheet.Cells.SetValue(0, 3, 4);
        Column(2, 5).Should().Equal(10, 20, 30, 40, null);
        _sheet.Cells.GetValue(0, 4).Should().Be(10);
    }

    [Test]
    public void Spill_Range_Of_A_Scalar_Cell_Is_Ref_Error()
    {
        _sheet.Cells.SetFormula(0, 2, "=SUM(A1#)");
        ErrorAt(0, 2).Should().Be(ErrorType.Ref);

        _sheet.Cells.SetFormula(0, 1, "={1;2}");
        _sheet.Cells.SetFormula(0, 2, "=SUM(B1#)");
        _sheet.Cells.GetValue(0, 2).Should().Be(3);
        _sheet.Cells.SetFormula(0, 1, "=5");
        ErrorAt(0, 2).Should().Be(ErrorType.Ref);
    }

    [Test]
    public void Copying_An_Anchor_Copies_The_Formula_Not_The_Spilled_Values()
    {
        _sheet.Cells.SetFormula(0, 1, "=A1:A3");
        _sheet.Commands.ExecuteCommand(new BlazorDatasheet.Core.Commands.Data.CopyRangeCommand(
            _sheet.Range(new Region(0, 2, 1, 1)), _sheet.Range(new Region(0, 2, 2, 2)), new CopyOptions()));

        _sheet.Cells.HasFormula(0, 2).Should().BeTrue();
        _sheet.Cells.HasFormula(1, 2).Should().BeFalse();
        Column(2, 3).Should().Equal(1, 2, 3);
        _sheet.Cells.IsSpilledCell(1, 2).Should().BeTrue();
    }

    [Test]
    public void Single_Value_Array_Results_Are_Written_As_Scalars()
    {
        _sheet.Cells.SetFormula(0, 1, "={7}");
        _sheet.Cells.GetValue(0, 1).Should().Be(7);
        _sheet.Cells.GetSpillStore().Any().Should().BeFalse();
    }
}
