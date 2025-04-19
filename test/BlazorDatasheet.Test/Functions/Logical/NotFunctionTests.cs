using System.Collections.Generic;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Functions.Logical;
using BlazorDatasheet.Test.Formula;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Functions.Logical;

public class NotFunctionTests
{
    private TestEnvironment _env;

    public object? Eval(string formulaString, bool resolveReferences = false)
    {
        var eval = new Evaluator(_env);
        var parser = new Parser(_env);
        var formula = parser.FromString(formulaString);
        return eval.Evaluate(formula, null, new FormulaEvaluationOptions(!resolveReferences)).Data;
    }


    [SetUp]
    public void Setup()
    {
        _env = new();
        _env.RegisterFunction("NOT", new NotFunction());
    }

    [Test]
    [TestCase("=NOT(true)", false)]
    [TestCase("=NOT(false)", true)]
    [TestCase("=NOT(1)", false)]
    [TestCase("=NOT(0)", true)]
    [TestCase("=NOT(4)", false)]
    public void Test_Not_With_Booleans(string formula, bool expectedValue)
    {
        Eval(formula).Should().Be(expectedValue);
    }

    [Test]
    public void Test_With_Str_Should_Be_Error()
    {
        Eval("""=NOT("A")""").Should().BeOfType<FormulaError>();
    }

    [Test]
    public void Not_With_Error_Should_Be_Error()
    {
        Eval("=NOT(#DIV/0!)").Should().BeOfType<FormulaError>();
    }
}