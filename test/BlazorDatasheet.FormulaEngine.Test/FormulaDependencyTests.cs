using BlazorDatasheet.FormulaEngine;
using NUnit.Framework;

namespace ExpressionEvaluator.Tests;

public class FormulaDependencyTests
{
    private FormulaEngine _engine;

    [SetUp]
    public void Test_Setup()
    {
        _engine = new FormulaEngine(new TestSheet());
    }

    [Test]
    public void Formula_Holds_Cell_References()
    {
        var formula = _engine.Parse("A1+A2");
        Assert.AreEqual(2, formula.References.Count());
    }

    [Test]
    public void Formula_Holds_Range_References()
    {
        var formula = _engine.Parse("A1:A2");
        Assert.AreEqual(1, formula.References.Count());
    }

    [Test]
    public void Formula_Holds_Named_References()
    {
        var formula = _engine.Parse("var+other");
        Assert.AreEqual(2, formula.References.Count());
    }

    [Test]
    public void Formula_Holds_Column_Range_References()
    {
        var formula = _engine.Parse("a:b");
        Assert.AreEqual(1, formula.References.Count());
    }

    [Test]
    public void Formula_Holds_Row_Range_References()
    {
        var formula = _engine.Parse("$3:4");
        Assert.AreEqual(1, formula.References.Count());
    }

    [Test]
    public void Repeat_Formula_Parsing_Has_Correct_References()
    {
        var formula = _engine.Parse("$3:4");
        Assert.AreEqual(1, formula.References.Count());
        var formula2 = _engine.Parse("A1+A2+A3");
        Assert.AreEqual(3, formula2.References.Count());
    }
}