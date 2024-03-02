using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using FluentAssertions;
using NUnit.Framework;
using Parser = BlazorDatasheet.Formula.Core.Interpreter.Parsing.Parser;

namespace BlazorDatasheet.Test.Formula;

public class InterpreterTests
{
    private Evaluator _evaluator;
    private TestEnvironment _env;
    private Parser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _evaluator = new Evaluator(_env);
        _parser = new Parser();
    }

    [Test]
    public void Cell_Reference_Gets_Cell_Value()
    {
        var str = "=A1";
        _env.SetCellValue(0, 0, 100);
        Assert.AreEqual(100, _evaluator.Evaluate(_parser.Parse(str)).Data);
    }

    [Test]
    public void Garbage_Cell_Ref_Formula_Returns_Error()
    {
        var formula = _parser.Parse("=A1..");
        _env.SetCellValue(0, 0, 100);
        Assert.True(_evaluator.Evaluate(formula).Data.GetType() == typeof(FormulaError));
    }

    [Test]
    public void Invalid_Str_Returns_Error()
    {
        var formula = _parser.Parse("=\"ABC");
        Assert.True(_evaluator.Evaluate(formula).Data.GetType() == typeof(FormulaError));
    }

    [Test]
    public void Invalid_Binary_Op_Returns_Error()
    {
        var formula = _parser.Parse("=\"A\"*\"B\"");
        Assert.True(_evaluator.Evaluate(formula).Data.GetType() == typeof(FormulaError));
    }

    [Test]
    public void Sum_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=7+9");
        Assert.AreEqual(16, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void Multiplication_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=4 * 5");
        Assert.AreEqual(20, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void Subtraction_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=9 - 6");
        Assert.AreEqual(3, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void Division_Constants_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=30/5");
        Assert.AreEqual(6, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void Divide_By_Zero_Returns_Formula_Error()
    {
        var formula = _parser.Parse("=10/0");
        var resError = (FormulaError)_evaluator.Evaluate(formula).Data!;
        Assert.NotNull(resError);
        Assert.AreEqual(ErrorType.Div0, resError!.ErrorType);
    }

    [Test]
    public void Negative_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=-30");
        Assert.AreEqual(-30, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void Paranthesis_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=(1+2)*3");
        Assert.AreEqual(9, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void Equate_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=6=6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula).Data);
        formula = _parser.Parse("=6=5");
        Assert.AreEqual(false, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    public void NotEqual_Operator_Evaluates_To_Correct_Value()
    {
        var formula = _parser.Parse("=6<>6");
        Assert.AreEqual(false, _evaluator.Evaluate(formula).Data);
        formula = _parser.Parse("=7<>6");
        Assert.AreEqual(true, _evaluator.Evaluate(formula).Data);
    }

    [Test]
    [TestCase("=1<2", true)]
    [TestCase("=1>2", false)]
    [TestCase("=1<=2", true)]
    [TestCase("=2<=2", true)]
    [TestCase("=3<=2", false)]
    [TestCase("=3>2", true)]
    [TestCase("=2>3", false)]
    [TestCase("=2>=3", false)]
    [TestCase("=3>=3", true)]
    [TestCase("=5>=3", true)]
    [TestCase("=true<false", false)]
    [TestCase("=false<true", true)]
    [TestCase("=\"a\"<\"b\"", true)]
    public void Greater_Than_Less_Than_Evaluates_To_Correct(string expr, bool expected)
    {
        EvalExpression(expr).Data.Should().Be(expected);
    }

    [Test]
    [TestCase("<")]
    [TestCase(".asd;")]
    [TestCase(".asd;")]
    [TestCase("a<")]
    [TestCase("3<<")]
    [TestCase("><><3<<")]
    [TestCase("+<3")]
    public void Garbage_Formulas_Result_In_Error(string exp)
    {
        EvalExpression(exp).IsError().Should().Be(true);
    }

    private CellValue EvalExpression(string expr)
    {
        return _evaluator.Evaluate(_parser.Parse(expr));
    }

    public void Test_Range()
    {
        var r = EvalExpression("=A1:A2");
    }

    [Test]
    public void Set_Variable_Evaluates_Variable()
    {
        _env.SetVariable("x", 10);
        EvalExpression("=x").Data.Should().Be(10);
    }

    [Test]
    public void Str_Concat_Binary_Op_Concats_Two_Strings()
    {
        EvalExpression($$"""="A"&"B" """).Data.Should().Be("AB");
    }

    [Test]
    public void Str_Concat_Binary_Op_Concats_Non_Strings()
    {
        EvalExpression($$"""="A"&2 """).Data.Should().Be("A2");
        EvalExpression($$"""="2"&"B" """).Data.Should().Be("2B");
    }

    [Test]
    public void Equality_Binary_Operations_With_Cell_References_Correctly_Works()
    {
        _env.SetCellValue(0, 0, 5);
        _env.SetCellValue(1, 0, 10);
        EvalExpression("=A1<A2").Data.Should().Be(true);
        EvalExpression("=A1>A2").Data.Should().Be(false);
        EvalExpression("=A1>=A2").Data.Should().Be(false);
        EvalExpression("=A1<=A2").Data.Should().Be(true);
        EvalExpression("=A1=A2").Data.Should().Be(false);
        EvalExpression("=A1<>A2").Data.Should().Be(true);
    }
}