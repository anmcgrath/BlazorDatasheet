using System;
using System.Collections.Generic;
using BlazorDatasheet.Model;
using BlazorDatasheet.Validation;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

[TestFixture]
public class DefaultValidators
{
    [Test]
    [TestCase(10)]
    [TestCase(-10.2)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void Number_Validator_Validates_Numbers(object value)
    {
        var validator = new NumberValidator(true);
        Assert.IsTrue(validator.IsValid(value));
    }

    [Test]
    [TestCase("10")]
    [TestCase("-10.2")]
    [TestCase("NaN")]
    public void Number_Validator_Validates_Strings(object val)
    {
        var validator = new NumberValidator(true);
        Assert.IsTrue(validator.IsValid(val));
    }

    [Test]
    [TestCase("abcd")]
    [TestCase("-10a")]
    public void Number_Validator_Invalidates_Incorrect_Strings(object val)
    {
        var validator = new NumberValidator(true);
        Assert.IsFalse(validator.IsValid(val));
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    [TestCase(null)]
    public void Number_Validator_Invalidates_Incorrect_Objects(object val)
    {
        var validator = new NumberValidator(true);
        var cell = new Cell(val);
        Assert.IsFalse(validator.IsValid(cell));
    }

    [Test]
    public void Number_Validator_Invalidates_Date()
    {
        var validator = new NumberValidator(true);
        var cell = new Cell(DateTime.MinValue);
        Assert.IsFalse(validator.IsValid(cell));
    }

    [Test]
    public void Source_Validator_Validates_String()
    {
        var items = new List<string>() { "Item1", "Item2" };
        var validator = new SourceValidator(items, false);
        var cell = new Cell("Item1");
        Assert.IsTrue(validator.IsValid(cell.GetValue()));
        cell.SetValue("Item3");
        Assert.IsFalse(validator.IsValid(cell.GetValue()));
        cell.SetValue("Item2");
        Assert.IsTrue(validator.IsValid(cell.GetValue()));

        cell.SetValue(100);
        Assert.IsFalse(validator.IsValid(cell.GetValue()));
    }

    [Test]
    public void Source_Validator_Validates_Numbers()
    {
        var items = new List<string>() { "1", "2", "100.2" };
        var validator = new SourceValidator(items, false);
        var cell = new Cell("a");
        Assert.IsFalse(validator.IsValid(cell.GetValue()));
        cell.SetValue(100.2);
        Assert.IsTrue(validator.IsValid(cell.GetValue()));
    }
}