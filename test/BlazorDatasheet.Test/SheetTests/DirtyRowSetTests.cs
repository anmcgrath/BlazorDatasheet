using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Intervals;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class DirtyRowSetTests
{
    [TestCase(0, int.MaxValue)]
    [TestCase(int.MinValue, 10)]
    public void Adding_An_Unbounded_Interval_Coalesces_Existing_Rows(int start, int end)
    {
        var rows = new DirtyRowSet();
        rows.Add(2, 2);
        rows.Add(5, 5);
        rows.Add(start, end);
        rows.Add(start, end);

        rows.Intervals.Should().ContainSingle().Which.Should().BeEquivalentTo(new Interval(start, end));
        rows.Covers(start, end).Should().BeTrue();
    }

    [Test]
    public void Adjacent_Intervals_At_The_Maximum_Row_Are_Merged()
    {
        var rows = new DirtyRowSet();
        rows.Add(int.MaxValue, int.MaxValue);
        rows.Add(int.MaxValue - 2, int.MaxValue - 1);

        rows.Intervals.Should().ContainSingle().Which
            .Should().BeEquivalentTo(new Interval(int.MaxValue - 2, int.MaxValue));
    }
}
