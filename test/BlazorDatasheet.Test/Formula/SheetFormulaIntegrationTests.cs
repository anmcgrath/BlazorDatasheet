using System.Collections.Generic;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.FormulaEngine;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class SheetFormulaIntegrationTests
{
    private Sheet _sheet;
    private FormulaEngine _engine;

    [SetUp]
    public void TestSetup()
    {
        _sheet = new Sheet(10, 10);
        _engine = _sheet.FormulaEngine;
    }

    [Test]
    public void Accept_Edit_With_Formula_String_Sets_Formula()
    {
        _sheet.SetCellValue(0, 0, 5);
        _sheet.Editor.BeginEdit(1, 1);
        _sheet.Editor.EditValue = "=A1 + 10";
        _sheet.Editor.AcceptEdit();

        Assert.IsTrue(_sheet.HasFormula(1, 1));
        _sheet.SetCellValue(0, 0, 5);
        Assert.AreEqual(15, _sheet.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Referenced_Cell_Value_Changes()
    {
        _sheet.SetFormula(1, 1, "=A1 + 10");
        _sheet.SetCellValue(0, 0, 5);
        Assert.AreEqual(15, _sheet.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Formula_Is_Set()
    {
        _sheet.SetCellValue(0, 0, 5);
        _sheet.SetFormula(1, 1, "=A1 + 10");
        Assert.AreEqual(15, _sheet.GetValue(1, 1));
    }

    [Test]
    public void Setting_Cell_Value_Will_Clear_Formula()
    {
        _sheet.SetFormula(1, 1, "=A1");
        Assert.IsTrue(_sheet.HasFormula(1, 1));

        // Set sheet cell (1, 1) to any old value and the formula should be cleared.
        _sheet.SetCellValue(1, 1, "Blah");
        Assert.IsFalse(_sheet.HasFormula(1, 1));
    }

    [Test]
    public void Clear_Sheet_Cell_Will_Clear_Formula()
    {
        _sheet.SetFormula(1, 1, "=A1");
        Assert.IsTrue(_sheet.HasFormula(1, 1));
        _sheet.ClearCells(_sheet.Range(1, 1));
        Assert.IsFalse(_sheet.HasFormula(1, 1));
    }

    [Test]
    public void Setting_Invalid_Formula_Will_Not_Set_Formula()
    {
        var invalidFormulaString = "=.A1";
        _sheet.SetFormula(0, 0, invalidFormulaString);
        Assert.False(_sheet.HasFormula(0, 0));
    }

    [Test]
    public void Set_Cell_Value_Over_Formula_Using_Command_Restores_On_Undo()
    {
        _sheet.SetCellValue(1, 1, "Test");
        _sheet.SetFormula(1, 1, "=10");
        Assert.IsTrue(_sheet.HasFormula(1, 1));
        Assert.AreEqual(10, _sheet.GetValue(1, 1));
        _sheet.Commands.ExecuteCommand(new SetCellValueCommand(1, 1, "TestChange"));
        _sheet.Commands.Undo();
        Assert.AreEqual(10, _sheet.GetValue(1, 1));
        Assert.IsTrue(_sheet.HasFormula(1, 1));
        Assert.AreEqual("=10", _sheet.GetFormulaString(1, 1));
    }

    [Test]
    public void Clear_Cell_Value_Using_Command_Restores_Formula_On_Undo()
    {
        _sheet.SetFormula(1, 1, "=10");
        _sheet.Commands.ExecuteCommand(new ClearCellsCommand(_sheet.Range(1, 1)));
        Assert.False(_sheet.HasFormula(1, 1));
        _sheet.Commands.Undo();
        Assert.True(_sheet.HasFormula(1, 1));
        Assert.AreEqual("=10", _sheet.GetFormulaString(1, 1));
    }

    [Test]
    public void Sum_On_Empty_Cell_Treats_Empty_Cell_As_Zero()
    {
        _sheet.SetFormula(1, 1, "=A1 + 5");
        Assert.AreEqual(5, _sheet.GetValue(1, 1));
    }

    [Test]
    public void FormulaEngine_Set_Variable_Calculates()
    {
        _engine.SetVariable("x", 10);
        _sheet.SetFormula(1, 1, "=x");
        _sheet.GetValue(1, 1).Should().Be(10);
    }
}