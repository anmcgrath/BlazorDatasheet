using System.Collections.Generic;
using BlazorDatasheet.DataStructures.Store;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class CumulativeRangeStoreTests
{
    private CumulativeRange1DStore store;
    private double defaultSize => store.Default;

    [SetUp]
    public void SetupStore()
    {
        store = new CumulativeRange1DStore(100);
    }

    [Test]
    public void Single_Size_Tests()
    {
        store.GetSize(0).Should().Be(defaultSize);
        store.GetSize(25).Should().Be(defaultSize);

        store.Set(0, 25);
        store.GetSize(0).Should().Be(25);
        store.GetSize(1).Should().Be(defaultSize);

        store.Set(10, 30);
        store.GetSize(10).Should().Be(30);
        store.GetSize(0).Should().Be(25);
        store.GetSize(9).Should().Be(defaultSize);
        store.GetSize(11).Should().Be(defaultSize);
    }

    [Test]
    public void CumulativeSizeTests_Default_Size_Only()
    {
        store.GetCumulative(0).Should().Be(0);
        store.GetCumulative(2).Should().Be(2 * defaultSize);
        store.GetSizeBetween(0, 1).Should().Be(defaultSize);
        store.GetSizeBetween(1, 2).Should().Be(defaultSize);
        store.GetSizeBetween(10, 20).Should().Be(10 * defaultSize);
    }

    [Test]
    public void CumulativeSizeTests_Set_Sizes()
    {
        store.Set(1, 30);
        store.GetSizeBetween(1, 2).Should().Be(30);
        store.GetCumulative(2).Should().Be(0 + defaultSize + 30);
        store.GetCumulative(3).Should().Be(0 + defaultSize + 30 + defaultSize);
        store.Set(3, 40);
        store.GetCumulative(3).Should().Be(0 + defaultSize + 30 + defaultSize);
        store.GetCumulative(4).Should().Be(0 + defaultSize + 30 + defaultSize + 40);
        store.GetCumulative(2).Should().Be(0 + defaultSize + 30);
    }

    [Test]
    public void Simple()
    {
        store.Set(0, 20);
        store.Set(1, 2, 30);
        store.Set(5, 40);
        store.GetSize(0).Should().Be(20);
    }

    [Test]
    public void GetIndexFromCumulativeTests()
    {
        // index  0   1    2    3
        // cumul  0   100  300  400
        store.GetPosition(-1).Should().Be(0);
        store.GetPosition(0).Should().Be(0);
        store.GetPosition(defaultSize - 1).Should().Be(0);
        store.GetPosition(defaultSize).Should().Be(1);

        // index 0    1     2     3
        // cumul 0    100   300   400
        store.Set(1, 2 * defaultSize);
        store.GetPosition(3 * defaultSize).Should().Be(2);
        store.GetPosition(defaultSize + 1).Should().Be(1);
        store.GetPosition(2 * defaultSize - 1).Should().Be(1);
        store.GetPosition(3 * defaultSize + 1).Should().Be(2);
        store.GetPosition(4 * defaultSize + 1).Should().Be(3);
    }

    [Test]
    public void Set_Size_To_Zero_Calculates_Correctly()
    {
        store.Set(1, defaultSize);
        store.Set(1, 0);
        store.GetSize(1).Should().Be(0);
        store.GetSizeBetween(0, 2).Should().Be(defaultSize);
        store.GetCumulative(3).Should().Be(2 * defaultSize);
    }

    [Test]
    public void Set_One_Then_Zero_Size_Doesnt_Crash()
    {
        Assert.DoesNotThrow(() =>
        {
            store.Set(1, 20);
            store.Set(0, 15);
            store.GetCumulative(0);
            store.GetCumulative(1);
            store.GetCumulative(2);
        });
    }

    [Test]
    public void Cut_Ranges_Calculates_Correctly()
    {
        store.Set(1, 100);
        store.Set(2, 200);
        store.Set(3, 300);
        store.GetCumulative(3).Should().Be(defaultSize + 100 + 200);
        store.Delete(2, 2);
        store.GetSize(2).Should().Be(300);
        store.GetSize(1).Should().Be(100);
        store.GetCumulative(3).Should().Be(defaultSize + 100 + 300);
    }

    [Test]
    public void Cut_Range_Index_That_is_Default_Correctly_Calculates()
    {
        store.Set(1, 100);
        store.Delete(0, 1);
        store.GetSize(0).Should().Be(100);
        store.GetSize(1).Should().Be(defaultSize);
        store.GetSize(2).Should().Be(defaultSize);
        store.GetCumulative(3).Should().Be(defaultSize * 2 + 100);
    }

    [Test]
    public void Insert_Range_index_Calculates()
    {
        store.Set(1, 100);
        store.Set(2, 200);
        store.Set(3, 300);
        store.InsertAt(2, 1);
        store.Set(2, 150);
        store.GetSize(1).Should().Be(100);
        store.GetSize(2).Should().Be(150);
        store.GetSize(3).Should().Be(200);
        store.GetCumulative(3).Should().Be(defaultSize + 100 + 150);
    }

    [Test]
    public void Setting_Values_Over_Default_Should_Return_Default_Changed()
    {
        var changed = store.Set(2, 5, 100);
        changed.AddedIntervals.Count.Should().Be(1);
        changed.AddedIntervals[0].Start.Should().Be(2);
        changed.AddedIntervals[0].End.Should().Be(5);
        changed.AddedIntervals[0].Data.Value.Should().Be(100);
    }

    [Test]
    public void Set_Cumulative_Values_Over_Multiple_Positions_Sets_Correctly()
    {
        int length = 5;
        double size = 40;
        store.Set(0, length - 1, size);
        for (int i = 0; i < length; i++)
        {
            store.GetCumulative(i).Should().Be(i * size);
            store.GetPosition(i * size + 1).Should().Be(i);
        }
    }
}