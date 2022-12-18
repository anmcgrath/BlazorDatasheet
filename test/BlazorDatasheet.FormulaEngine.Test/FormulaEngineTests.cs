using BlazorDatasheet.FormulaEngine;
using NUnit.Framework;

namespace ExpressionEvaluator.Tests;

public class FormulaEvaluatorTests
{
    [Test]
    public void Register_Formula_Registers_Ok()
    {
        var sheet = new TestSheet();
        sheet.SetValue(0, 0, 10);
        sheet.SetValue(1, 0, 12);
        var engine = new FormulaEngine(sheet);

        var formula = engine.Parse("A1+A2");

        engine.SetFormula(0, 1, formula);
        Assert.AreEqual(22, sheet.GetCell(0,1).Value);

        sheet.SetValue(0, 0, 12);
        engine.OnUpdateValue(0, 0);
        
        Assert.AreEqual(20, sheet.GetCell(0,1).Value);
    }
}