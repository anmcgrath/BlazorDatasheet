using BlazorDatasheet.Edit;
using BlazorDatasheet.Formula.Core.Interpreter;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Functions.HintboxTests;

public class HintBoxCalculator
{
    [Test]
    [TestCase(2, -1, "sum")]
    [TestCase(4, -1, "sum")]
    [TestCase(5, 0, "sum")]
    [TestCase(6, 0, "sum")]
    [TestCase(7, 1, "sum")]
    [TestCase(8, 1, "sum")]
    [TestCase(9, 2, "sum")]
    [TestCase(10, 2, "sum")]
    public void Basic_Function_Provides_Correct_Arg_Index(int cursorPosition, int expectedArgIndex,
        string expectedFuncName)
    {
        var func = "=sum(1,2,3)";
        var result = new FormulaHintBoxCalculator(new FormulaOptions()).Calculate(func, cursorPosition);
        result.Should().NotBeNull();
        result!.ParameterIndex.Should().Be(expectedArgIndex);
        result!.FunctionName.Should().Be(expectedFuncName);
    }

    [Test]
    public void Position_Outside_Of_Formula_Returns_null()
    {
        var func = "=1 + sum(1,2, 3) + 2";
        var result = new FormulaHintBoxCalculator(new FormulaOptions()).Calculate(func, 0);
        result.Should().BeNull();
        result = new FormulaHintBoxCalculator(new FormulaOptions()).Calculate(func, 5);
        result.Should().BeNull();
        result = new FormulaHintBoxCalculator(new FormulaOptions()).Calculate(func, 16);
        result.Should().BeNull();
    }

    [Test]
    [TestCase(5, 0, "a")]
    [TestCase(7, 1, "a")]
    [TestCase(8, -1, "b")]
    [TestCase(9, 0, "b")]
    [TestCase(11, 1, "b")]
    [TestCase(13, 1, "a")]
    [TestCase(14, 2, "a")]
    public void Nested_Function_Produces_Correct_ArgIndex(int cursorPosition, int expectedArgIndex,
        string expectedFuncName)
    {
        var func = "=  a(1,b(1,2),3)";
        var result = new FormulaHintBoxCalculator(new FormulaOptions()).Calculate(func, cursorPosition);
        result.Should().NotBeNull();
        result!.ParameterIndex.Should().Be(expectedArgIndex);
        result!.FunctionName.Should().Be(expectedFuncName);
    }
}