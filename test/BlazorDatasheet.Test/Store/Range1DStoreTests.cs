using System;
using BlazorDatasheet.DataStructures.Store;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class Range1DStoreTests
{
    [Test]
    public void Insert_At_Shifts_Right()
    {
        var r1str = "Range 1";
        var r2str = "Range 2";
        var store = new Range1DStore<string>(string.Empty);
        store.Set(0, 10,r1str);
        store.Set(15, 20, r2str);
        store.Get(15).Should().Be(r2str);
        store.InsertAt(12, 2);
        store.Get(10).Should().Be(r1str);
        store.Get(11).Should().Be(string.Empty);
        store.Get(14).Should().Be(string.Empty);
        store.Get(15).Should().Be(String.Empty);
        store.Get(17).Should().Be(r2str);
        store.Get(22).Should().Be(r2str);
        store.Get(23).Should().Be(string.Empty);
    }
    
    [Test]
    public void Insert_At_Shifts_Right_Only_One_Interval_Defined()
    {
        var r1str = "Range 1";
        var store = new Range1DStore<string>(string.Empty);
        store.Set(0, 10,r1str);
        store.InsertAt(12, 1);
        store.Get(10).Should().Be(r1str);
        store.Get(11).Should().Be(string.Empty);
    }
}