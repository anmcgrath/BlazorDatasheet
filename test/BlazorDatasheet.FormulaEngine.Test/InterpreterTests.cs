using BlazorDatasheet.FormulaEngine.Interpreter;
using ExpressionEvaluator;
using NUnit.Framework;

namespace BlazorDatasheet.FormulaEngine.Test;

public class InterpreterTests
{
    private FormulaEngine _engine;
    private FormulaEvaluator _evaluator;

    [SetUp]
    public void Setup()
    {
        var sheet = new TestSheet();
        _engine = new FormulaEngine(sheet);
        _evaluator = new FormulaEvaluator(new Environment(sheet));
    }

    [Test]
    public void Sum_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("7+9");
        Assert.AreEqual(16, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Multiplication_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("4 * 5");
        Assert.AreEqual(20, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Subtraction_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("9 - 6");
        Assert.AreEqual(3, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Division_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("30/5");
        Assert.AreEqual(6, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Negative_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("-30");
        Assert.AreEqual(-30, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Paranthesis_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("(1+2)*3");
        Assert.AreEqual(9, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Equate_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("6=6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula));
        formula = _engine.Parse("6=5");
        Assert.AreEqual(false, _evaluator.Evaluate(formula));
    }

    [Test]
    public void NotEqual_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _engine.Parse("6<>6");
        Assert.AreEqual(false, _evaluator.Evaluate(formula));
        formula = _engine.Parse("7<>6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula));
    }
}