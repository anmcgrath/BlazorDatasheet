using System;
using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;
using FluentAssertions;
using NUnit.Framework;
using Parser = BlazorDatasheet.Formula.Core.Interpreter.Parsing.Parser;

namespace BlazorDatasheet.Test.Functions;

public class MathFunctionTests
{
    private TestEnvironment _env;

    [SetUp]
    public void Setup()
    {
        _env = new();
    }

    public object? Eval(string formulaString)
    {
        var eval = new Evaluator(_env);
        var parser = new Parser();
        return eval.Evaluate(parser.Parse(formulaString)).Data;
    }

    [Test]
    public void Sin_Function_Tests()
    {
        _env.RegisterFunction("sin", new SinFunction());
        Eval("=sin(true)").Should().Be(Math.Sin(1));
        _env.SetCellValue(0, 0, true);
        Eval("=sin(A1)").Should().Be(Math.Sin(1));
        Eval("=sin(\"abc\")").Should().BeOfType(typeof(FormulaError));
        _env.SetCellValue(0, 0, "abc");
        Eval("=sin(A1)").Should().BeOfType(typeof(FormulaError));
    }

    [Test]
    public void Sum_Function_Tests()
    {
        _env.RegisterFunction("sum", new SumFunction());
        var res1 = Eval("=sum(1, 2)");
        res1.Should().Be(3);
        Eval("=sum(1/0)").Should().BeOfType<FormulaError>();
        Eval("=sum(5)").Should().Be(5);
        Eval("=sum(true,true)").Should().Be(2);
        Eval("=sum(\"ab\",true)").Should().BeOfType<FormulaError>();
        Eval("=sum({1,2,3},4)").Should().Be(10);

        var nums = new double[] { 0.5, 1, 1.5, 2 };

        _env.SetCellValue(0, 0, nums[0]);
        _env.SetCellValue(1, 0, nums[1]);
        _env.SetCellValue(0, 2, nums[2]);
        _env.SetCellValue(1, 2, nums[3]);

        Eval("=sum(A1:A2,C1:C2)").Should().Be(nums.Sum());

        _env.SetCellValue(0, 0, true);
        Eval("=sum(A1)").Should().Be(0);

        _env.SetCellValue(0, 0, "abc");
        Eval("=sum(A1)").Should().Be(0);

        _env.SetCellValue(2, 1, 123);
        Eval("=sum(B3)").Should().Be(123);
    }

    [Test]
    public void Intercept_Function_Tests()
    {
        _env.RegisterFunction("intercept", new InterceptFunction());
        // ys
        _env.SetCellValue(0, 0, 1d);
        _env.SetCellValue(1, 0, 3d);
        _env.SetCellValue(2, 0, 4d);
        _env.SetCellValue(4, 0, 100d);

        // xs
        _env.SetCellValue(0, 1, 0d);
        _env.SetCellValue(1, 1, 1d);
        _env.SetCellValue(2, 1, 2d);
        _env.SetCellValue(4, 0, true);

        var intercept = Eval("=intercept(A1:A3,B1:B3)") as double?;
        intercept.Should().NotBeNull().And.BeApproximately(7 / 6d, 0.00001d);

        Eval("=intercept(A1:A4,B1:B3)").Should()
            .BeOfType<FormulaError>("The array number of rows are not the same.");
        Eval("=intercept(A1:B4,A1:A4)").Should()
            .BeOfType<FormulaError>("The array number of columns are not the same.");

        intercept = Eval("=intercept(A1:A5,B1:B5)") as double?;
        intercept.Should().NotBeNull().And.BeApproximately(7 / 6d, 0.00001d,
            because: "Row 4 col 0 value is skipped because it doesn't have a corresponding number value");
    }

    [Test]
    public void Slope_Function_Tests()
    {
        _env.RegisterFunction("slope", new SlopeFunction());
        // ys
        _env.SetCellValue(0, 0, 1d);
        _env.SetCellValue(1, 0, 3d);
        _env.SetCellValue(2, 0, 4d);
        _env.SetCellValue(4, 0, 100d);

        // xs
        _env.SetCellValue(0, 1, 0d);
        _env.SetCellValue(1, 1, 1d);
        _env.SetCellValue(2, 1, 2d);
        _env.SetCellValue(4, 0, true);

        var slope = Eval("=slope(A1:A3,B1:B3)") as double?;
        slope.Should().NotBeNull().And.BeApproximately(3 / 2d, 0.00001d);

        Eval("=slope(A1:A4,B1:B3)").Should()
            .BeOfType<FormulaError>("The array number of rows are not the same.");
        Eval("=slope(A1:B4,A1:A4)").Should()
            .BeOfType<FormulaError>("The array number of columns are not the same.");

        slope = Eval("=slope(A1:A5,B1:B5)") as double?;
        slope.Should().NotBeNull().And.BeApproximately(3 / 2d, 0.00001d,
            because: "Row 4 col 0 value is skipped because it doesn't have a corresponding number value");
    }
}