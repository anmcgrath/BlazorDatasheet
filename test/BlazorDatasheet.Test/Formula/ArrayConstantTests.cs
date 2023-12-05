using System.Collections.Generic;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class ArrayConstantTests
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
    public void Array_Constant_Parses_As_Array_Expression()
    {
        var formula = _parser.FromString("={1,2,3}");
        var res = (CellValue[][])_evaluator.Evaluate(formula)!;
        res.Should().BeOfType<CellValue[][]>();
        res.Length.Should().Be(1);
        res[0].Length.Should().Be(3);
        res[0][0].Data.Should().Be(1);
        res[0][1].Data.Should().Be(2);
        res[0][2].Data.Should().Be(3);
    }

    [Test]
    public void Multi_Row_Array_Constant_Parses_As_Array_Expression()
    {
        var formula = _parser.FromString("={1,2;3,4}");
        var res = (CellValue[][])_evaluator.Evaluate(formula)!;
        res.Should().BeOfType<CellValue[][]>();
        res.Length.Should().Be(2);
        res[0].Length.Should().Be(2);
        res[1].Length.Should().Be(2);
        res[0][0].Data.Should().Be(1);
        res[0][1].Data.Should().Be(2);
        res[1][0].Data.Should().Be(3);
        res[1][1].Data.Should().Be(4);
    }

    [Test]
    public void Invalid_Row_Length_Fails_Validation()
    {
        var formula = _parser.FromString("={1,2;3,4,5}");
        formula.IsValid().Should().Be(false);
    }
}