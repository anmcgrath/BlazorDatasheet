using System.Linq;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class IntervalsTest
{
    [Test]
    [TestCase(1,5,3,true)]
    [TestCase(1,5,5,true)]
    [TestCase(1,5,1,true)]
    [TestCase(1,5,6,false)]
    [TestCase(1,5,0,false)]
    [TestCase(1,1,0,false)]
    [TestCase(1,1,1,true)]
    public void Interval_Contains_Tests(int start, int end, int value, bool expected)
    {
        var interval = new OrderedInterval(start, end);
        Assert.AreEqual(expected, interval.Contains(value));
    }

    [Test]
    [TestCase(0,1,1,2,true)]
    [TestCase(0,1,2,2,false)]
    [TestCase(1,1,0,2,true)]
    [TestCase(2 ,1,-5,2,true)]
    [TestCase(2 ,1,2,-5,true)]
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

        var merge1 = OrderedInterval.Merge(i1, i2).OrderBy(x=>x.Start).ToList();
        Assert.AreEqual(1, merge1.Count);
        Assert.AreEqual(6, merge1.First().Length);
        Assert.AreEqual(0, merge1.First().Start);
        Assert.AreEqual(5, merge1.First().End);

        var merge2 = OrderedInterval.Merge(i1, i2, i3).OrderBy(x=>x.Start).ToList();
        Assert.AreEqual(2, merge2.Count);
        Assert.AreEqual(6, merge2[0].Length);
        Assert.AreEqual(3, merge2[1].Length);

        var merge3 = OrderedInterval.Merge(i1, i2, i3, i4).OrderBy(x=>x.Start).ToList();
        Assert.AreEqual(1, merge3.Count);
        Assert.AreEqual(31, merge3.First().Length);
        Assert.AreEqual(-10, merge3.First().Start);
        Assert.AreEqual(20, merge3.First().End);
    }
}