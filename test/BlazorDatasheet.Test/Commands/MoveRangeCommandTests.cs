using System.Linq;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MoveRangeCommandTests
{
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(20, 20);
    }

    private bool Move(string from, int toRow, int toCol, Sheet? target = null)
    {
        target ??= _sheet;
        return target.Commands.ExecuteCommand(new MoveRangeCommand(_sheet.Range(from)!, target.Range(toRow, toCol)));
    }

    [Test]
    public void Move_Moves_Values_Formats_Merges_Types_And_Clears_Source()
    {
        _sheet.Cells.SetValue(0, 0, "a");
        _sheet.Cells.SetValue(1, 0, "b");
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _sheet.Cells.Merge(new Region(2, 3, 0, 0));
        _sheet.Cells.SetType(new Region(1, 0), "boolean");

        Move("A1:A4", 0, 4).Should().BeTrue();

        _sheet.Cells.GetValue(0, 4).Should().Be("a");
        _sheet.Cells.GetValue(1, 4).Should().Be("b");
        _sheet.GetFormat(0, 4).BackgroundColor.Should().Be("red");
        _sheet.Cells.GetMerge(2, 4).Should().BeEquivalentTo(new Region(2, 3, 4, 4));
        _sheet.Cells.GetCellType(1, 4).Should().Be("boolean");

        _sheet.Cells.GetValue(0, 0).Should().BeNull();
        _sheet.Cells.GetValue(1, 0).Should().BeNull();
        _sheet.GetFormat(0, 0).BackgroundColor.Should().BeNull();
        _sheet.Cells.GetMerge(2, 0).Should().BeNull();
        _sheet.Cells.GetCellType(1, 0).Should().Be("default");
    }

    [Test]
    public void Dependent_Outside_Block_Is_Rewritten_And_Recalculates()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetFormula(0, 2, "=A1+A2");

        Move("A1:A2", 0, 4);

        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=E1+E2");
        _sheet.Cells.GetValue(0, 2).Should().Be(3);
        _sheet.Cells.SetValue(0, 4, 10);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);
    }

    [Test]
    public void Formula_Inside_Block_Referencing_Inside_Moves_With_It()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetFormula(1, 0, "=A1*2");

        Move("A1:A2", 0, 4);

        _sheet.Cells.GetFormulaString(1, 4).Should().Be("=E1*2");
        _sheet.Cells.GetValue(1, 4).Should().Be(2);
        _sheet.Cells.HasFormula(1, 0).Should().BeFalse();
    }

    [Test]
    public void Formula_Inside_Block_Referencing_Outside_Is_Unchanged()
    {
        _sheet.Cells.SetValue(0, 1, 5);
        _sheet.Cells.SetFormula(0, 0, "=B1");

        Move("A1", 4, 4);

        _sheet.Cells.GetFormulaString(4, 4).Should().Be("=B1");
        _sheet.Cells.GetValue(4, 4).Should().Be(5);
        _sheet.Cells.SetValue(0, 1, 6);
        _sheet.Cells.GetValue(4, 4).Should().Be(6);
    }

    [Test]
    public void Partially_Overlapping_Reference_Is_Unchanged()
    {
        _sheet.Cells.SetFormula(0, 2, "=sum(A1:A3)");
        Move("A1:A2", 0, 4);
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=sum(A1:A3)");
    }

    [Test]
    public void Fixed_References_Move()
    {
        _sheet.Cells.SetValue(0, 0, 3);
        _sheet.Cells.SetFormula(0, 2, "=$A$1");
        Move("A1", 4, 4);
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=$E$5");
        _sheet.Cells.GetValue(0, 2).Should().Be(3);
    }

    [Test]
    public void Move_Over_Own_Source_Shifts_Values()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetValue(2, 0, 3);
        _sheet.Cells.SetFormula(1, 1, "=A2");

        Move("A1:A2", 1, 0);

        _sheet.Cells.GetValue(0, 0).Should().BeNull();
        _sheet.Cells.GetValue(1, 0).Should().Be(1);
        _sheet.Cells.GetValue(2, 0).Should().Be(2);
        _sheet.Cells.GetFormulaString(1, 1).Should().Be("=A3");

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
        _sheet.Cells.GetValue(1, 0).Should().Be(2);
        _sheet.Cells.GetValue(2, 0).Should().Be(3);
        _sheet.Cells.GetFormulaString(1, 1).Should().Be("=A2");
    }

    [Test]
    public void Move_Across_Sheets_Qualifies_References()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Sheet1", 10, 10);
        var s2 = wb.AddSheet("Sheet2", 10, 10);
        s1.Cells.SetValue(0, 0, 1); // A1
        s1.Cells.SetValue(0, 1, 4); // B1
        s1.Cells.SetFormula(1, 0, "=B1"); // A2, inside block, references outside
        s1.Cells.SetFormula(0, 2, "=A1"); // C1, outside block, references inside

        s2.Commands.ExecuteCommand(new MoveRangeCommand(s1.Range("A1:A2")!, s2.Range(1, 1))).Should().BeTrue();

        s2.Cells.GetValue(1, 1).Should().Be(1);
        s2.Cells.GetFormulaString(2, 1).Should().Be("=Sheet1!B1");
        s2.Cells.GetValue(2, 1).Should().Be(4);
        s1.Cells.GetFormulaString(0, 2).Should().Be("=Sheet2!B2");
        s1.Cells.GetValue(0, 2).Should().Be(1);
        s1.Cells.GetValue(0, 0).Should().BeNull();
        s1.Cells.HasFormula(1, 0).Should().BeFalse();

        s2.Cells.SetValue(1, 1, 7);
        s1.Cells.GetValue(0, 2).Should().Be(7);

        s2.Commands.Undo(); // the value set
        s2.Commands.Undo(); // the move
        s1.Cells.GetValue(0, 0).Should().Be(1);
        s1.Cells.GetFormulaString(1, 0).Should().Be("=B1");
        s1.Cells.GetFormulaString(0, 2).Should().Be("=A1");
        s2.Cells.GetValue(1, 1).Should().BeNull();
        s2.Cells.HasFormula(2, 1).Should().BeFalse();
        s1.Cells.SetValue(0, 0, 9);
        s1.Cells.GetValue(0, 2).Should().Be(9);
    }

    [Test]
    public void Undo_Restores_Source_Target_Dependents_And_Graph_In_One_Step()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetFormula(0, 2, "=A1+A2");
        _sheet.Cells.SetValue(0, 4, "old");
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _sheet.Cells.Merge(new Region(0, 1, 1, 1)); // B1:B2

        Move("A1:B2", 0, 4);
        _sheet.Commands.Undo();

        _sheet.Cells.GetValue(0, 0).Should().Be(1);
        _sheet.Cells.GetValue(1, 0).Should().Be(2);
        _sheet.Cells.GetValue(0, 4).Should().Be("old");
        _sheet.Cells.GetValue(1, 4).Should().BeNull();
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=A1+A2");
        _sheet.GetFormat(0, 0).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(0, 4).BackgroundColor.Should().BeNull();
        _sheet.Cells.GetMerge(0, 1).Should().BeEquivalentTo(new Region(0, 1, 1, 1));
        _sheet.Cells.GetMerge(0, 5).Should().BeNull();

        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);

        Move("A1:B2", 0, 4);
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=E1+E2");
        _sheet.Cells.GetValue(0, 4).Should().Be(10);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);
    }

    [Test]
    public void Move_To_Same_Place_Does_Nothing()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        Move("A1:A2", 0, 0).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }

    [Test]
    public void Move_Refuses_ReadOnly_Target()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.SetFormat(new Region(5, 5), new CellFormat { IsReadOnly = true });
        Move("A1", 5, 5).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }

    [Test]
    public void Move_Refuses_Locked_Cells_When_Protected()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.SetFormat(new Region(0, 0), new CellFormat { IsLocked = true });
        _sheet.SetFormat(new Region(5, 5), new CellFormat { IsLocked = false });
        _sheet.Protection.Protect();
        Move("A1", 5, 5).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }

    [Test]
    public void Move_Does_Not_Change_The_Callers_Options()
    {
        var options = new CopyOptions();
        _sheet.Commands.ExecuteCommand(new MoveRangeCommand(_sheet.Range("A1")!, _sheet.Range(2, 2), options));
        options.FormulaReferences.Should().Be(ReferenceAdjustment.Relative);
    }
}
