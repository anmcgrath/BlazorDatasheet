using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
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
        var parser = new Parser(_env, new FormulaOptions());
        var evaluator = new Evaluator(_env);
        var formulaStr = @"=SUM(1,2;2;0,3)";
        var formula = parser.Parse(formulaStr);
        evaluator.Evaluate(formula).Data.Should().Be(1.2 + 2 + 0.3);
        $"={formula.Root.ToExpressionText()}".Should().Be(formulaStr);
    }


    [Test]
    [SetCulture("fr-FR")]
    public void Non_Eng_Culture_Has_Correct_Array_Defn_Separators()
    {
        var parser = new Parser(_env, new FormulaOptions());
        var evaluator = new Evaluator(_env);
        var formulaStr = @"={1\2;3\4}";
        var formula = parser.Parse(formulaStr);
        var res = (CellValue[][])evaluator.Evaluate(formula).Data!;
        res.Should().BeOfType<CellValue[][]>();
        res.Length.Should().Be(2);
        res[0].Length.Should().Be(2);
        res[1].Length.Should().Be(2);
        res[0][0].Data.Should().Be(1);
        res[0][1].Data.Should().Be(2);
        res[1][0].Data.Should().Be(3);
        res[1][1].Data.Should().Be(4);
        $"={formula.Root.ToExpressionText()}".Should().Be(formulaStr);
    }

    [Test]
    [SetCulture("fr-FR")]
    public void Provide_Custom_Func_Param_Seperator_Should_Eval_Ok()
    {
        _env.RegisterFunction("SUM", new SumFunction());
        var options = new FormulaOptions()
        {
            SeparatorSettings = new SeparatorSettings()
            {
                FuncParameterSeparator = '.',
            }
        };
        EvalExpression("=SUM(1,2. 3.4)", options).Data.Should().Be(1.2 + 3 + 4);
    }
}