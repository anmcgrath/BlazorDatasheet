using System.Linq;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Intervals;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal.Execution;

namespace BlazorDatasheet.Test.Geometry;

public class IntervalsTest
{
    [Test]
    [TestCase(1, 5, 3, true)]
    [TestCase(1, 5, 5, true)]
    [TestCase(1, 5, 1, true)]
    [TestCase(1, 5, 6, false)]
    [TestCase(1, 5, 0, false)]
    [TestCase(1, 1, 0, false)]
    [TestCase(1, 1, 1, true)]
    public void Interval_Contains_Tests(int start, int end, int value, bool expected)
    {
        var interval = new OrderedInterval(start, end);
        Assert.AreEqual(expected, interval.Contains(value));
    }

    [Test]
    [TestCase(0, 1, 1, 2, true)]
    [TestCase(0, 1, 2, 2, false)]
    [TestCase(1, 1, 0, 2, true)]
    [TestCase(2, 1, -5, 2, true)]
    [TestCase(2, 1, 2, -5, true)]
    public void Interval_Intersection_Tests(int i1_start, int i1_end, int i2_start, int i2_end, bool overlaps)
    {
        var i1 = new OrderedInterval(i1_start, i1_end);
        var i2 = new OrderedInterval(i2_start, i2_end);
        Assert.AreEqual(overlaps, i1.Overlaps(i2));
        Assert.AreEqual(overlaps, i2.Overlaps(i1));
    }

    [Test]
    public void Merge_Intervals_Tests()
    {
        var i1 = new OrderedInterval(0, 2);
        var i2 = new OrderedInterval(1, 5);
        var i3 = new OrderedInterval(7, 9);
        var i4 = new OrderedInterval(-10, 20);

        var merge1 = OrderedInterval.Merge(i1, i2).OrderBy(x => x.Start).ToList();
        Assert.AreEqual(1, merge1.Count);
        Assert.AreEqual(6, merge1.First().Length);
        Assert.AreEqual(0, merge1.First().Start);
        Assert.AreEqual(5, merge1.First().End);

        var merge2 = OrderedInterval.Merge(i1, i2, i3).OrderBy(x => x.Start).ToList();
        Assert.AreEqual(2, merge2.Count);
        Assert.AreEqual(6, merge2[0].Length);
        Assert.AreEqual(3, merge2[1].Length);

        var merge3 = OrderedInterval.Merge(i1, i2, i3, i4).OrderBy(x => x.Start).ToList();
        Assert.AreEqual(1, merge3.Count);
        Assert.AreEqual(31, merge3.First().Length);
        Assert.AreEqual(-10, merge3.First().Start);
        Assert.AreEqual(20, merge3.First().End);
    }

    [Test]
    public void Add_Ordered_Intervals_To_Non_Overlappying_Interval_Store_Correct()
    {
        var store = new MergeableIntervalStore<CellFormat>();
        var f1 = new CellFormat()
        {
            BackgroundColor = "f1",
            ForegroundColor = "f1"
        };
        var f2 = new CellFormat()
        {
            BackgroundColor = "f2",
            FontWeight = "f2"
        };
        store.Add(new OrderedInterval<CellFormat>(0, 1, f1));
        store.Add(new OrderedInterval<CellFormat>(1, 2, f2));

        var fStore1 = store.Get(0);
        Assert.NotNull(fStore1);
        Assert.NotNull(fStore1.BackgroundColor);
        Assert.AreEqual(f1.BackgroundColor, fStore1.BackgroundColor);
        Assert.AreEqual(f1.ForegroundColor, fStore1.ForegroundColor);

        var fStore2 = store.Get(1);
        Assert.NotNull(fStore2);
        Assert.NotNull(fStore2.ForegroundColor);
        Assert.NotNull(fStore2.BackgroundColor);
        Assert.NotNull(fStore2.FontWeight);
        Assert.AreEqual(f1.ForegroundColor, fStore2.ForegroundColor);
        Assert.AreEqual(f2.BackgroundColor, fStore2.BackgroundColor);
        Assert.AreEqual(f2.FontWeight, fStore2.FontWeight);

        var fStore3 = store.Get(2);
        Assert.NotNull(fStore3);
        Assert.Null(fStore3.ForegroundColor);
        Assert.NotNull(fStore3.BackgroundColor);
        Assert.NotNull(fStore3.FontWeight);
        Assert.AreEqual(f2.BackgroundColor, fStore3.BackgroundColor);
        Assert.AreEqual(f2.FontWeight, fStore3.FontWeight);
    }

    [Test]
    public void Cut_Ordered_Interval_From_Non_Overlappting_Store_Correct()
    {
        var val = new SimpleMergeableData<int>()
        {
            Value = 10
        };
        var store = new MergeableIntervalStore<SimpleMergeableData<int>>();
        store.Add(new OrderedInterval<SimpleMergeableData<int>>(0, 10, val));
        var cuts = store.Clear(new OrderedInterval(1, 9));
        Assert.AreEqual(val, store.Get(0));
        Assert.Null(store.Get(1));
        Assert.Null(store.Get(5));
        Assert.Null(store.Get(9));
        Assert.AreEqual(val, store.Get(10));

        cuts.Count.Should().Be(1);
        cuts[0].Start.Should().Be(1);
        cuts[0].End.Should().Be(9);
    }

    [Test]
    public void Cut_Ordered_Interval_From_Non_overlapping_Store_REturns_Correct_Cuts()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<int>>();
        store.Add(1, 3, new SimpleMergeableData<int>(-1));
        store.Add(5, 6, new SimpleMergeableData<int>(1));
        store.Add(8, 10, new SimpleMergeableData<int>(2));

        var removed = store.Clear(2, 9);
        removed.Count.Should().Be(3);
        removed = removed.OrderBy(x => x.Start).ToList();
        removed[0].Start.Should().Be(2);
        removed[0].End.Should().Be(3);
        removed[0].Data.Value.Should().Be(-1);

        removed[1].Start.Should().Be(5);
        removed[1].End.Should().Be(6);
        removed[1].Data.Value.Should().Be(1);

        removed[2].Start.Should().Be(8);
        removed[2].End.Should().Be(9);
        removed[2].Data.Value.Should().Be(2);
    }

    [Test]
    public void Add_Interval_Between_Sets_Correctly()
    {
        var format1 = new SimpleMergeableData<string>() { Value = "f1" };
        var format2 = new SimpleMergeableData<string>() { Value = "f2" };
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(new OrderedInterval<SimpleMergeableData<string>>(0, 2, format1));
        store.Add(new OrderedInterval<SimpleMergeableData<string>>(1, 1, format2));
        Assert.AreEqual(format1.Value, store.Get(0)?.Value);
        Assert.AreEqual(format2.Value, store.Get(1)?.Value);
        Assert.AreEqual(format1.Value, store.Get(2)?.Value);
    }

    [Test]
    public void Modified_Intervals_returned_After_adding_When_Interval_Contained_InExisting()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(0, 10, new SimpleMergeableData<string>("init")).Should().NotBeEmpty();

        var modified = store.Add(2, 3, new SimpleMergeableData<string>("new"));
        modified.Count.Should().Be(1);
        modified[0].Data.Value.Should().Be("init");
        modified[0].Start.Should().Be(2);
        modified[0].End.Should().Be(3);
    }

    [Test]
    public void Modified_Intervals_returned_After_adding_When_Overlaps_Partially()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(1, 3, new SimpleMergeableData<string>("init_start"))
            .Should().NotBeEmpty()
            .And.Subject.First().Data.Should().Be(default(string));

        store.Add(5, 7, new SimpleMergeableData<string>("init_end"))
            .Should().NotBeEmpty()
            .And.Subject.First().Data.Should().Be(default(string));

        var modified = store.Add(2, 6, new SimpleMergeableData<string>("new"));
        modified.Count.Should().Be(2);
        modified[0].Data.Value.Should().Be("init_start");
        modified[0].Start.Should().Be(2);
        modified[0].End.Should().Be(3);

        modified[1].Data.Value.Should().Be("init_end");
        modified[1].Start.Should().Be(5);
        modified[1].End.Should().Be(6);
    }

    [Test]
    public void Shift_Right_Shifts_To_Right_by_n()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(0, 2, new SimpleMergeableData<string>("start"));
        store.Add(4, 5, new SimpleMergeableData<string>("mid"));
        store.Add(7, 10, new SimpleMergeableData<string>("end"));
        store.ShiftRight(4, 2);
        var allIntervals = store.GetAllIntervals().ToList();
        var shift = 2;
        allIntervals[0].End.Should().Be(2);
        allIntervals[1].Start.Should().Be(4 + shift);
        allIntervals[1].End.Should().Be(5 + shift);
        allIntervals[2].Start.Should().Be(7 + shift);
        allIntervals[2].End.Should().Be(10 + shift);
    }

    [Test]
    public void NonOverlappingStore_Start_End_Gets_Updated()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(2, 10, new SimpleMergeableData<string>(""));
        store.Start.Should().Be(2);
        store.End.Should().Be(10);
        store.Clear(0, 4);
        store.Start.Should().Be(5);
        store.Clear(9, 11);
        store.End.Should().Be(8);
    }

    [Test]
    public void Shift_Right_Inside_Extends_Interval()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(4, 6, new SimpleMergeableData<string>(""));
        store.ShiftRight(5, 5);
        store.GetIntervals(4, 4).First().Start.Should().Be(4);
        store.GetIntervals(4, 4).First().End.Should().Be(6 + 5);
    }

    [Test]
    public void Shift_Left_Shifts_NonOverlaps_ByN()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(5, 10, new SimpleMergeableData<string>());
        store.Add(15, 20, new SimpleMergeableData<string>());
        var shift = 2;
        store.ShiftLeft(6, shift);
        var all = store.GetAllIntervals();
        all[0].Start.Should().Be(5);
        all[0].End.Should().Be(10 - shift);
        all[1].Start.Should().Be(15 - shift);
        all[1].End.Should().Be(20 - shift);
    }

    [Test]
    public void Clear_MergeableIntervalStore_Clears_Store()
    {
        var store = new MergeableIntervalStore<SimpleMergeableData<string>>();
        store.Add(5, 10, new SimpleMergeableData<string>());
        store.Clear();
        store.GetAllIntervals().Should().BeEmpty();
    }
}

public class SimpleMergeableData<T> : IMergeable<SimpleMergeableData<T>>
{
    public T Value { get; set; }

    public SimpleMergeableData(T value = default(T))
    {
        Value = value;
    }

    public void Merge(SimpleMergeableData<T> item)
    {
        Value = item.Value;
    }

    public SimpleMergeableData<T> Clone()
    {
        return new SimpleMergeableData<T>()
        {
            Value = Value
        };
    }
}