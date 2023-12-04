using System.Collections.Generic;
using BlazorDatasheet.Formula.Core.Regression;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula.Regression;

public class LinearRegressionTests
{
    [Test]
    public void Linear_Function_Calculates_Correct_Value()
    {
        var l = new LinearFunction(0.5, 2);
        l.ComputeY(10).Should().Be(7);
        l.ComputeX(5).Should().Be(6);
    }

    [Test]
    public void Linear_Regression_Two_Data_Points_Correct()
    {
        var r = new LinearRegression();
        var x = new List<double>() { 0d, 5d };
        var y = new List<double>() { 2d, 12d };
        var l = r.Calculate(x, y);
        l.Gradient.Should().Be(2);
        l.YIntercept.Should().Be(2);
    }

    [Test]
    public void Linear_Regression_flat_Line_Correct()
    {
        var r = new LinearRegression();
        var x = new List<double>() { 0d, 5d };
        var y = new List<double>() { 5d, 5d };
        var l = r.Calculate(x, y);
        l.Gradient.Should().Be(0);
        l.YIntercept.Should().Be(5);
    }

    [Test]
    public void Linear_Regression_Three_Data_Points_Correct()
    {
        var r = new LinearRegression();
        var x = new List<double>() { 0d, 1d, 2d };
        var y = new List<double>() { 1d, 3d, 4d };
        var l = r.Calculate(x, y);
        l.Gradient.Should().Be(1.5);
        l.YIntercept.Should().BeApproximately(7 / 6d, 0.00001d);
    }
}