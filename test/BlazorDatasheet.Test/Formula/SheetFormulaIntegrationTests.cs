using System.Collections.Generic;
using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class SheetFormulaIntegrationTests
{
    private Sheet _sheet;
    private FormulaEngine.FormulaEngine _engine;

    [SetUp]
    public void TestSetup()
    {
        _sheet = new Sheet(10, 10);
        _engine = _sheet.FormulaEngine;
    }

    [Test]
    public void Accept_Edit_With_Formula_String_Sets_Formula()
    {
        _sheet.TrySetCellValue(0, 0, 5);
        _sheet.Editor.BeginEdit(1, 1);
        _sheet.Editor.EditValue = "=A1 + 10";
        _sheet.Editor.AcceptEdit();

        Assert.IsTrue(_engine.HasFormula(1, 1));
        _sheet.TrySetCellValue(0, 0, 5);
        Assert.AreEqual(15, _sheet.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Referenced_Cell_Value_Changes()
    {
        var formulaSet = _engine.SetFormulaString(1, 1, "=A1 + 10");
        Assert.IsTrue(formulaSet);
        _sheet.TrySetCellValue(0, 0, 5);
        Assert.AreEqual(15, _sheet.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Formula_Is_Set()
    {
        _sheet.TrySetCellValue(0, 0, 5);
        _engine.SetFormulaString(1, 1, "=A1 + 10");
        Assert.AreEqual(15, _sheet.GetValue(1, 1));
    }

    [Test]
    public void Setting_Cell_Value_Will_Clear_Formula()
    {
        _engine.SetFormulaString(1, 1, "=A1");
        Assert.IsTrue(_engine.HasFormula(1, 1));

        // Set sheet cell (1, 1) to any old value and the formula should be cleared.
        _sheet.TrySetCellValue(1, 1, "Blah");
        Assert.IsFalse(_engine.HasFormula(1, 1));
    }

    [Test]
    public void Clear_Sheet_Cell_Will_Clear_Formula()
    {
        _engine.SetFormulaString(1, 1, "=A1");
        Assert.IsTrue(_engine.HasFormula(1, 1));
        _sheet.ClearCells(_sheet.Range(1, 1));
        Assert.IsFalse(_engine.HasFormula(1, 1));
    }

    [Test]
    public void Setting_Invalid_Formula_Will_Not_Set_Formula()
    {
        var invalidFormulaString = "=.A1";
        var isSet = _engine.SetFormulaString(0, 0, invalidFormulaString);
        Assert.False(isSet);
        Assert.False(_engine.HasFormula(0, 0));
    }

    [Test]
    public void Set_Cell_Value_Using_Command_Over_Formula_Restores_On_Undo()
    {
        _sheet.TrySetCellValue(1, 1, "Test");
        _engine.SetFormulaString(1, 1, "=10");
        Assert.IsTrue(_engine.HasFormula(1, 1));
        Assert.AreEqual(10, _sheet.GetValue(1, 1));
        _sheet.Commands.ExecuteCommand(new SetCellValuesCommand(new List<CellChange>()
        {
            new CellChange(1, 1, "TestChange")
        }));
        _sheet.Commands.Undo();
        Assert.AreEqual(10, _sheet.GetValue(1, 1));
        Assert.IsTrue(_engine.HasFormula(1, 1));
        Assert.AreEqual("=10", _engine.GetFormulaString(1, 1));
    }

    [Test]
    public void Clear_Cell_Value_Using_Command_Restores_Formula_On_Undo()
    {
        _engine.SetFormulaString(1, 1, "=10");
        _sheet.Commands.ExecuteCommand(new ClearCellsCommand(_sheet.Range(1, 1)));
        Assert.False(_engine.HasFormula(1, 1));
        _sheet.Commands.Undo();
        Assert.True(_engine.HasFormula(1, 1));
        Assert.AreEqual("=10", _engine.GetFormulaString(1, 1));
    }

    [Test]
    public void Sum_On_Empty_Cell_Treats_Empty_Cell_As_Zero()
    {
        _engine.SetFormulaString(1, 1, "=A1 + 5");
        Assert.AreEqual(5, _sheet.GetValue(1, 1));
    }
}