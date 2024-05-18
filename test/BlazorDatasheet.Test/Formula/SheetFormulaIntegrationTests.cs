using System.Collections.Generic;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class SheetFormulaIntegrationTests
{
    private Sheet _sheet;

    [SetUp]
    public void TestSetup()
    {
        _sheet = new Sheet(10, 10);
    }

    [Test]
    public void Accept_Edit_With_Formula_String_Sets_Formula()
    {
        _sheet.Cells.SetValue(0, 0, 5);
        _sheet.Editor.BeginEdit(1, 1);
        _sheet.Editor.EditValue = "=A1 + 10";
        _sheet.Editor.AcceptEdit();

        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        _sheet.Cells.SetValue(0, 0, 5);
        var formulaVal = _sheet.Cells.GetValue(1, 1);
        Assert.AreEqual(15, formulaVal);
    }

    [Test]
    public void Formula_Calculation_Performs_When_Referenced_Cell_Value_Changes()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1 + 10");
        _sheet.Cells.SetValue(0, 0, 5);
        Assert.AreEqual(15, _sheet.Cells.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Formula_Is_Set()
    {
        _sheet.Cells.SetValue(0, 0, 5);
        _sheet.Cells.CellsChanged += (sender, positions) =>
        {
            var x = 2;
        };
        _sheet.Cells.SetFormula(1, 1, "=A1 + 10");
        Assert.AreEqual(15, _sheet.Cells.GetValue(1, 1));
    }

    [Test]
    public void Setting_Cell_Value_Will_Clear_Formula()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1");
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));

        // Set sheet cell (1, 1) to any old value and the formula should be cleared.
        _sheet.Cells.SetValue(1, 1, "Blah");
        Assert.IsFalse(_sheet.Cells.HasFormula(1, 1));
    }

    [Test]
    public void Clear_Sheet_Cell_Will_Clear_Formula()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1");
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        _sheet.Cells.ClearCells(new Region(1, 1));
        Assert.IsFalse(_sheet.Cells.HasFormula(1, 1));
    }

    [Test]
    public void Setting_Invalid_Formula_Will_Not_Set_Formula()
    {
        var invalidFormulaString = "=.A1";
        _sheet.Cells.SetFormula(0, 0, invalidFormulaString);
        Assert.False(_sheet.Cells.HasFormula(0, 0));
    }

    [Test]
    public void Set_Cell_Value_Over_Formula_Using_Command_Restores_On_Undo()
    {
        _sheet.Cells.SetValue(1, 1, "Test");
        _sheet.Cells.SetFormula(1, 1, "=10");
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        Assert.AreEqual(10, _sheet.Cells.GetValue(1, 1));
        _sheet.Commands.ExecuteCommand(new SetCellValueCommand(1, 1, "TestChange"));
        _sheet.Commands.Undo();
        Assert.AreEqual(10, _sheet.Cells.GetValue(1, 1));
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        Assert.AreEqual("=10", _sheet.Cells.GetFormulaString(1, 1));
    }

    [Test]
    public void Clear_Cell_Value_Using_Command_Restores_Formula_On_Undo()
    {
        _sheet.Cells.SetFormula(1, 1, "=10");
        _sheet.Commands.ExecuteCommand(new ClearCellsCommand(_sheet.Range(1, 1)));
        Assert.False(_sheet.Cells.HasFormula(1, 1));
        _sheet.Commands.Undo();
        Assert.True(_sheet.Cells.HasFormula(1, 1));
        Assert.AreEqual("=10", _sheet.Cells.GetFormulaString(1, 1));
    }

    [Test]
    public void Sum_On_Empty_Cell_Treats_Empty_Cell_As_Zero()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1 + 5");
        var val = _sheet.Cells.GetValue(1, 1);
        val.Should().Be(5);
    }

    [Test]
    public void Set_Cell_Formula_Over_Value_Then_Undo_Restores_Value()
    {
        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.SetFormula(0, 0, "=5");
        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(0, 0).Should().Be(10);
    }


    [Test]
    public void FormulaEngine_Set_Variable_Calculates()
    {
        _sheet.FormulaEngine.SetVariable("x", 10);
        _sheet.Cells.SetFormula(1, 1, "=x");
        _sheet.Cells.GetValue(1, 1).Should().Be(10);
    }

    [Test]
    public void Formula_Referencing_Range_With_Formula_Recalcs_When_Formula_Recalcs()
    {
        // Cell A1 = 10
        // Cell A2 = 20
        // Cell A3 = Sum(A1:A2)
        // Cell A4 = Sum(A2:A3)
        // When we set A1 and A2, A3 should evaluate first and then A4 because the result of A4 depends on A3
        _sheet.Cells.SetFormula(2, 0, "=AVERAGE(A1:A2)");
        _sheet.Cells.SetFormula(3, 0, "=AVERAGE(A2:A3)");
        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.SetValue(1, 0, 20);
        _sheet.Cells.GetValue(2, 0).Should().Be((10 + 20) / 2d);
        _sheet.Cells.GetValue(3, 0).Should().Be((15 + 20) / 2d);
    }

    [Test]
    public void Formula_Referencing_Deleted_Formula_Updates_When_Formula_Is_Cleared_Then_Value_Changes()
    {
        _sheet.Cells.SetFormula(0, 0, "=A2");
        _sheet.Cells.SetFormula(0, 1, "=A1");
        _sheet.Cells.ClearCells(new Region(0, 0));
        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.GetCellValue(0, 1).GetValue<int>().Should().Be(10);
    }

    [Test]
    public void Formula_Referencing_Deleted_Formula_Updates_When_Formula_Has_Value_Set_Over_It()
    {
        _sheet.Cells.SetFormula(0, 0, "=A2");
        _sheet.Cells.SetFormula(0, 1, "=A1");

        // now override the formula in A1, the formula in B1 (0,1) should update to the new value
        _sheet.Cells.SetValue(0, 0, 10);

        _sheet.Cells.GetCellValue(0, 1).GetValue<int>().Should().Be(10);
    }

    [Test]
    public void Sheet_Should_Not_Recalculate_If_Formula_Removed_From_Sheet()
    {
        _sheet.Cells[0, 0].Formula = "=B1"; // set A1 = B1
        _sheet.Cells[1, 0].Formula = "=A1"; // set A2 = A1
        // override formula at A1 - should remove links from A1 -> B1
        _sheet.Cells.SetValue(0, 0, string.Empty);
        // set value at b1 and ensure sheet doesn't calculate

        int changeCount = 0;

        _sheet.Cells.CellsChanged += (sender, args) =>
        {
            changeCount++;
        };
        // change B1
        _sheet.Cells[0, 1].Value = 2;
        
        changeCount.Should().Be(1);
    }
}