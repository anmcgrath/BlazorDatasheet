using ExpressionEvaluator;
using NUnit.Framework;

namespace BlazorDatasheet.FormulaEngine.Test;

public class FormulaEvaluatorTests
{
    /*[Test]
    public void Register_Formula_Registers_Ok()
    {
        var sheet = new TestSheet();
        sheet.SetValue(0, 0, 10);
        sheet.SetValue(1, 0, 12);
        var engine = new FormulaEngine(sheet);

        var formula = engine.Parse("A1+A2");
        Assert.AreEqual(2, formula.References.Count());
        engine.SetFormula(0, 1, formula);
        Assert.AreEqual(22, sheet.GetCell(0, 1).Value);
        Assert.AreEqual(1, engine.GetDependents(0, 0).Count());
        Assert.AreEqual(1, engine.GetDependents(1, 0).Count());
    }

    [Test]
    public void Update_Cells_With_Dependents_Updates_Formula()
    {
        var sheet = new TestSheet();
        sheet.SetValue(0, 0, 10);
        sheet.SetValue(1, 0, 12);
        var engine = new FormulaEngine(sheet);

        var formula = engine.Parse("A1+A2");
        engine.SetFormula(0, 1, formula);

        sheet.SetValue(0, 0, 20);
        engine.CalculateDependents(0, 0);

        Assert.AreEqual(32, sheet.GetCell(0, 1).Value);
    }*/
}