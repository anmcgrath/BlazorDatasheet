using BlazorDatasheet.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;
using BlazorDatasheet.FormulaEngine;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class InterpreterTests
{
    private FormulaEvaluator _evaluator;
    private TestEnvironment _env;
    private FormulaParser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _evaluator = new FormulaEvaluator(_env);
        _parser = new FormulaParser();
    }

    [Test]
    public void Cell_Reference_Gets_Cell_Value()
    {
        var str = "=A1";
        _env.SetCellValue(0, 0, 100);
        Assert.AreEqual(100, _evaluator.Evaluate(_parser.FromString(str)));
    }

    [Test]
    public void Garbage_Cell_Ref_Formula_Returns_Error()
    {
        var formula = _parser.FromString("=A1..");
        _env.SetCellValue(0, 0, 100);
        Assert.True(_evaluator.Evaluate(formula).GetType() == typeof(FormulaError));
    }

    [Test]
    public void Invalid_Str_Returns_Error()
    {
        var formula = _parser.FromString("=\"ABC");
        Assert.True(_evaluator.Evaluate(formula).GetType() == typeof(FormulaError));
    }

    [Test]
    public void Invalid_Binary_Op_Returns_Error()
    {
        var formula = _parser.FromString("=\"A\"*\"B\"");
        Assert.True(_evaluator.Evaluate(formula).GetType() == typeof(FormulaError));
    }

    [Test]
    public void Sum_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=7+9");
        Assert.AreEqual(16, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Multiplication_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=4 * 5");
        Assert.AreEqual(20, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Subtraction_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=9 - 6");
        Assert.AreEqual(3, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Division_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=30/5");
        Assert.AreEqual(6, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Negative_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=-30");
        Assert.AreEqual(-30, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Paranthesis_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=(1+2)*3");
        Assert.AreEqual(9, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Equate_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=6=6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula));
        formula = _parser.FromString("=6=5");
        Assert.AreEqual(false, _evaluator.Evaluate(formula));
    }

    [Test]
    public void NotEqual_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.FromString("=6<>6");
        Assert.AreEqual(false, _evaluator.Evaluate(formula));
        formula = _parser.FromString("=7<>6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula));
    }
}