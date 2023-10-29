using System.Collections.Generic;
using BlazorDatashet.Formula.Functions.Logical;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Functions;

public class LogicalFunctionTests
{
    [Test]
    public void IfFunctionTests()
    {
        var ifFunction = new IfFunction();
        ifFunction.Call(new List<object>() { true }).Should().Be(true);
        ifFunction.Call(new List<object>() { false }).Should().Be(false);
        ifFunction.Call(new List<object>() { true, "yes", "no", }).Should().Be("yes");
        ifFunction.Call(new List<object>() { false, "yes", "no", }).Should().Be("no");
        ifFunction.Call(new List<object>() { true, "yes" }).Should().Be("yes");
        ifFunction.Call(new List<object>() { false, "yes" }).Should().Be(false);
    }

    [Test]
    public void AndFunctionTests()
    {
        var andFunction = new AndFunction();
        andFunction.Call(new List<object>() { true }).Should().Be(true);
        andFunction.Call(new List<object>() { false }).Should().Be(false);
        andFunction.Call(new List<object>() { true, false }).Should().Be(false);
        andFunction.Call(new List<object>() { true, true }).Should().Be(true);
    }
}