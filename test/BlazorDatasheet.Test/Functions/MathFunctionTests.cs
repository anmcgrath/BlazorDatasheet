using System;
using System.Collections.Generic;
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
        sinFunction.Call(new List<object>() { 0.5 }).Should().Be(Math.Sin(0.5));
    }

    [Test]
    public void SumFunctionTests()
    {
        var sumFunction = new SumFunction();
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
        }).Should().Be(10);
    }
}