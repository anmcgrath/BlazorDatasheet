using System.Globalization;
using System.Threading;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatashet.Formula.Functions.Math;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class InterpretorCultureTests
{
    private IEnvironment _env;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
    }

    private CellValue EvalExpression(string expr, FormulaOptions? options = null)
    {
        var parser = new Parser(_env, options);
        var evaluator = new Evaluator(_env);
        return evaluator.Evaluate(parser.Parse(expr));
    }

    [Test]
    [SetCulture("fr-FR")]
    public void Non_Eng_Culture_Has_Correct_Decimal_Separator()
    {
        EvalExpression("=0,1").GetValue<double>().Should().BeApproximately(0.1, 1e-6);
        EvalExpression("=,2").GetValue<double>().Should().BeApproximately(0.2, 1e-6);
    }

    [Test]
    [SetCulture("fr-FR")]
    public void Non_Eng_Culture_Has_Correct_Function_Param_Separator()
    {
        _env.RegisterFunction("SUM", new SumFunction());
        EvalExpression("=SUM(1;2;3)").Data.Should().Be(6);
    }
}