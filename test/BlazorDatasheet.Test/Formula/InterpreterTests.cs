using BlazorDatasheet.Data;
using BlazorDatasheet.FormulaEngine;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class InterpreterTests
{
    private FormulaEvaluator _evaluator;
    private Parser _parser;
    private Sheet _sheet;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(3, 3);
        _parser = new Parser();
        _evaluator = new FormulaEvaluator(new Environment(_sheet));
    }

    [Test]
    public void Cell_Reference_Gets_Cell_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=A1");
        _sheet.TrySetCellValue(0, 0, 100);
        Assert.AreEqual(100, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Garbage_Cell_Ref_Formula_Returns_Error()
    {
        var formula = _parser.Parse(new Lexer(), "=A1..");
        _sheet.TrySetCellValue(0, 0, 100);
        Assert.True(_evaluator.Evaluate(formula).GetType() == typeof(FormulaError));
    }

    [Test]
    public void Invalid_Str_Returns_Error()
    {
        var formula = _parser.Parse(new Lexer(), "=\"ABC");
        Assert.True(_evaluator.Evaluate(formula).GetType() == typeof(FormulaError));
    }

    [Test]
    public void Invalid_Binary_Op_Returns_Error()
    {
        var formula = _parser.Parse(new Lexer(), "=\"A\"*\"B\"");
        Assert.True(_evaluator.Evaluate(formula).GetType() == typeof(FormulaError));
    }

    [Test]
    public void Sum_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=7+9");
        Assert.AreEqual(16, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Multiplication_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=4 * 5");
        Assert.AreEqual(20, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Subtraction_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=9 - 6");
        Assert.AreEqual(3, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Division_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=30/5");
        Assert.AreEqual(6, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Negative_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=-30");
        Assert.AreEqual(-30, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Paranthesis_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=(1+2)*3");
        Assert.AreEqual(9, _evaluator.Evaluate(formula));
    }

    [Test]
    public void Equate_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=6=6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula));
        formula = _parser.Parse(new Lexer(), "=6=5");
        Assert.AreEqual(false, _evaluator.Evaluate(formula));
    }

    [Test]
    public void NotEqual_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse(new Lexer(), "=6<>6");
        Assert.AreEqual(false, _evaluator.Evaluate(formula));
        formula = _parser.Parse(new Lexer(), "=7<>6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula));
    }
}