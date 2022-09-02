using System;
using BlazorDatasheet.Model;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class CellTests
{
    [Test]
    public void Get_Value_Gets_Simple_Value_Of_Cell()
    {
        const string expected = "test_string";
        var cell = new Cell(expected);
        var val = cell.GetValue<string>();
        Assert.AreEqual(expected, val);
        Assert.AreNotEqual("random_string", val);
    }

    [Test]
    public void Casting_Value_As_Incorrect_Throws_Exception()
    {
        var cell = new Cell("test_string");
        Assert.Throws<NullReferenceException>(() =>
        {
            var val = cell.GetValue<int>();
        });
    }

    [Test]
    public void Cell_With_Key_Returns_Correct_Value()
    {
        var testObject = new TestObject() { Value = "test_string" };
        var cell = new Cell(testObject, key: nameof(testObject.Value));
        Assert.AreEqual("test_string", cell.GetValue<string>());
    }

    [Test]
    public void Setting_Cell_Value_Then_Getting_Returns_Correct_Value()
    {
        var cell = new Cell("init");
        Assert.AreEqual("init", cell.GetValue());
        cell.SetValue(100);
        Assert.AreEqual(100, cell.GetValue<int>());
        Assert.AreEqual("100", cell.GetValue<string>());
        Assert.AreEqual(100, cell.GetValue());

        cell.SetValue(false);
        Assert.AreEqual(false, cell.GetValue<bool>());
    }

    [Test]
    public void Setting_Cell_Value_Then_Getting_Returns_Correct_Value_With_Key()
    {
        var testObject = new TestObject() { Value = "init" };
        var cell = new Cell(testObject, nameof(testObject.Value));
        Assert.AreEqual("init", cell.GetValue());
        cell.SetValue(100);
        Assert.AreEqual(100, cell.GetValue<int>());
        Assert.AreEqual("100", cell.GetValue<string>());
        Assert.AreEqual("100", cell.GetValue());
        cell.SetValue(false);
        Assert.AreEqual(false, cell.GetValue<bool>());
        Assert.AreEqual(false.ToString(), testObject.Value);
    }

    [Test]
    public void Does_Not_Set_Value_If_Incorrect_Type()
    {
        var testObject = new TestObject2() { Value = 100 };
        var cell = new Cell(testObject, nameof(testObject.Value));
        var hasSetValue = cell.SetValue("abc");
        Assert.IsFalse(hasSetValue);
        Assert.AreEqual(100, testObject.Value);
    }
}

internal class TestObject
{
    public string Value { get; set; }
}

internal class TestObject2
{
    public int Value { get; set; }
}