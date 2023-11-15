using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.DataStructures.Cells;
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
    [Test]
    public void SinFunctionTests()
    {
        var sinFunction = new SinFunction();
        sinFunction.Call(new FuncArg[]
                { new FuncArg(new CellValue(true), sinFunction.GetParameterDefinitions().First()) }).Should()
            .Be(Math.Sin(1));
        sinFunction.Call(new FuncArg[]
                { new FuncArg(new CellValue(0.5), sinFunction.GetParameterDefinitions().First()) }).Should()
            .Be(Math.Sin(0.5));
    }

    [Test]
    public void Test_Env_sin_function()
    {
        var env = new TestEnvironment();
        env.SetFunction("sin", new SinFunction());
        var parser = new FormulaParser();
        var formula1 = parser.FromString("=sin(true)");
        var formula2 = parser.FromString("=sin(A1)");
        var formula3 = parser.FromString("=sin(\"abc\")");
        var eval = new FormulaEvaluator(env);
        env.SetCellValue(0, 0, true);
        eval.Evaluate(formula1).Should().Be(Math.Sin(1));
        eval.Evaluate(formula2).Should().Be(Math.Sin(1));
        eval.Evaluate(formula3).Should().BeOfType(typeof(FormulaError));
    }

    [Test]
    public void Test_Env_Sum_function()
    {
        var env = new TestEnvironment();
        env.SetFunction("sum", new SumFunction());
        var parser = new FormulaParser();
        var eval = new FormulaEvaluator(env);
        eval.Evaluate(parser.FromString("=sum(1, 2)")).Should().Be(3);
        eval.Evaluate(parser.FromString("=sum(true,true)")).Should().Be(2);
    }

    [Test]
    public void SumFunctionTests()
    {
        /*var sumFunction = new SumFunction();
        sumFunction.Call(new List<object>()
        {
            new List<double>() { 0 }
        }).Should().Be(0);

        sumFunction.Call(new List<object>()
        {
            new List<double>() { 1, 2 }
        }).Should().Be(3);

        sumFunction.Call(new List<object>()
        {
            new List<double>() { 1, 2, },
            new List<double>() { 3, 4 },
        }).Should().Be(10);*/
    }
}