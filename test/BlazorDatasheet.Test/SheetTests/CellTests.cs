using System;
using BlazorDatasheet.Core.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

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
    public void Setting_Cell_Value_Then_Getting_Returns_Correct_Value()
    {
        var cell = new Cell("init");
        Assert.AreEqual("init", cell.GetValue());
        cell.TrySetValue(100);
        Assert.AreEqual(100, cell.GetValue<int>());
        Assert.AreEqual("100", cell.GetValue<string>());
        Assert.AreEqual(100, cell.GetValue());

        cell.TrySetValue(false);
        Assert.AreEqual(false, cell.GetValue<bool>());
    }

    [Test]
    public void Empty_Cell_Has_Null_Value()
    {
        var cell = new Cell();
        Assert.IsNull(cell.GetValue());
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