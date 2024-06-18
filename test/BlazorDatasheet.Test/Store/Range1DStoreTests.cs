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
        store.Set(0, 10, r1str);
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
        store.Set(0, 10, r1str);
        store.InsertAt(12, 1);
        store.Get(10).Should().Be(r1str);
        store.Get(11).Should().Be(string.Empty);
    }

    [Test]
    public void Clear_Range_Clears_Data()
    {
        var store = new Range1DStore<bool>(true);
        store.Set(4, 11, false);
        int clearStart = 6;
        int clearEnd = 9;
        store.Clear(clearStart, clearEnd);
        for (int i = 4; i <= 11; i++)
        {
            store.Get(i).Should().Be(i >= clearStart && i <= clearEnd);
        }
    }

    [Test]
    public void Get_Next_Range_Gets_Correct_Interval()
    {
        var store = new Range1DStore<bool>(false);
        store.Set(5, 10, true);
        store.Set(15, 20, true);
        store.GetNext(0).Should().BeEquivalentTo((5, 10, true));
        store.GetNext(7).Should().BeEquivalentTo((15, 20, true));
        store.GetNext(10).Should().BeEquivalentTo((15, 20, true));
        store.GetNext(20).Should().BeNull();
    }

    [Test]
    public void Get_Next_Range_In_reverse_Gets_Correct_Interval()
    {
        var store = new Range1DStore<bool>(false);
        store.Set(5, 10, true);
        store.Set(15, 20, true);
        store.GetNext(0, -1).Should().BeNull();
        store.GetNext(7, -1).Should().BeNull();
        store.GetNext(11, -1).Should().BeEquivalentTo((5, 10, true));
        store.GetNext(17, -1).Should().BeEquivalentTo((5, 10, true));
        store.GetNext(22, -1).Should().BeEquivalentTo((15, 20, true));
    }
    
    [Test]
    public void Get_Next_Range_With_Empty_Store_Returns_Null()
    {
        var store = new Range1DStore<bool>(false);
        store.GetNext(5, -1).Should().BeNull();
        store.GetNext(5, 1).Should().BeNull();
    }
}