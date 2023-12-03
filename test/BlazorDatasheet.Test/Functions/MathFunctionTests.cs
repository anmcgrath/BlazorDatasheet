using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;
using FluentAssertions;
using NUnit.Framework;

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
        var eval = new FormulaEvaluator(_env);
        var parser = new FormulaParser();
        return eval.Evaluate(parser.FromString(formulaString));
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
        Eval("=sum(1, 2)").Should().Be(3);
        Eval("=sum(5)").Should().Be(5);
        Eval("=sum(true,true)").Should().Be(2);
        Eval("=sum(\"ab\",true)").Should().BeOfType<FormulaError>();

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
}