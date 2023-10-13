using BlazorDatasheet.DataStructures.Store;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class CumulativeRangeStoreTests
{
    [Test]
    public void Single_Size_Tests()
    {
        var defaultSize = 20;
        var store = new CumulativeRangeStore(defaultSize);
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
        var defaultSize = 20;
        var store = new CumulativeRangeStore(defaultSize);
        store.GetCumulative(0).Should().Be(0);
        store.GetCumulative(2).Should().Be(2 * defaultSize);
        store.GetSizeBetween(0, 1).Should().Be(defaultSize);
        store.GetSizeBetween(1, 2).Should().Be(20);
        store.GetSizeBetween(10, 20).Should().Be(10 * defaultSize);
    }

    [Test]
    public void CumulativeSizeTests_Set_Sizes()
    {
        var defaultSize = 20;
        var store = new CumulativeRangeStore(defaultSize);
        store.Set(1, 30);
        store.GetSizeBetween(1, 2).Should().Be(30);
        store.GetCumulative(2).Should().Be(0 + 30);
        store.GetCumulative(3).Should().Be(0 + 30 + defaultSize);
        store.Set(3, 40);
        store.GetCumulative(3).Should().Be(0 + 30 + defaultSize);
        store.GetCumulative(4).Should().Be(0 + 30 + defaultSize + 40);
    }
}