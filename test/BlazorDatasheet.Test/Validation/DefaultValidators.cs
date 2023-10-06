using System;
using System.Collections.Generic;
using BlazorDatasheet.Data;
using BlazorDatasheet.Validation;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Validation;

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
        Assert.IsFalse(validator.IsValid(DateTime.MinValue));
    }

    [Test]
    public void Source_Validator_Validates_String()
    {
        var items = new List<string>() { "Item1", "Item2" };
        var validator = new SourceValidator(items, false);
        Assert.IsTrue(validator.IsValid("Item1"));
        Assert.IsFalse(validator.IsValid("Item3"));
        Assert.IsTrue(validator.IsValid("Item2"));
        Assert.IsFalse(validator.IsValid(100));
    }

    [Test]
    public void Source_Validator_Validates_Numbers()
    {
        var items = new List<string>() { "1", "2", "100.2" };
        var validator = new SourceValidator(items, false);
        Assert.IsFalse(validator.IsValid("a"));
        Assert.IsTrue(validator.IsValid(100.2));
        Assert.IsFalse(validator.IsValid(5));
    }
}