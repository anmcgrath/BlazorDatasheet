using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class RegionTests
{
    [Test]
    public void Create_Region_With_Single_Cell_Size_Sets_Correct_Row_Cols()
    {
        // Create a region at row = 10, col = 11 with size 1
        var region = new Region(10, 11);
        Assert.AreEqual(10, region.TopLeft.Row);
        Assert.AreEqual(10, region.BottomRight.Row);
        Assert.AreEqual(11, region.TopLeft.Col);
        Assert.AreEqual(11, region.BottomRight.Col);
    }

    [Test]
    public void Create_Region_With_Specific_Starts_And_Ends_Creates_With_Correct_Row_Cols()
    {
        var region = new Region(1, 2, 3, 4);
        Assert.AreEqual(1, region.TopLeft.Row);
        Assert.AreEqual(2, region.BottomRight.Row);
        Assert.AreEqual(3, region.TopLeft.Col);
        Assert.AreEqual(4, region.BottomRight.Col);
    }

    [Test]
    public void Constrain_To_Smaller_Region_Constrains_Correctly()
    {
        var large = new Region(0, 5, 1, 6);
        var small = new Region(2, 4, 3, 5);
        large.Constrain(small);
        Assert.AreEqual(small.TopLeft.Row, large.TopLeft.Row);
        Assert.AreEqual(small.BottomRight.Row, large.BottomRight.Row);
        Assert.AreEqual(small.TopLeft.Col, large.TopLeft.Col);
        Assert.AreEqual(small.BottomRight.Col, large.BottomRight.Col);
    }

    [Test]
    public void Constrain_To_Larger_Region_Constrains_Correctly()
    {
        var large = new Region(0, 5, 1, 6);
        var small = new Region(2, 4, 3, 5);
        small.Constrain(large);
        Assert.AreEqual(2, small.TopLeft.Row);
        Assert.AreEqual(4, small.BottomRight.Row);
        Assert.AreEqual(3, small.TopLeft.Col);
        Assert.AreEqual(5, small.BottomRight.Col);
    }

    [Test]
    public void Width_Height_Area_Ok()
    {
        var region = new Region(1, 4, 2, 7);
        Assert.AreEqual(4, region.Height);
        Assert.AreEqual(6, region.Width);
        Assert.AreEqual(24, region.Area);
    }

    [Test]
    public void Backwards_Region_Height_Width_Ok()
    {
        var region = new Region(4, 1, 7, 2);
        Assert.AreEqual(4, region.Height);
        Assert.AreEqual(6, region.Width);
        Assert.AreEqual(24, region.Area);
    }

    [Test]
    public void Contains_Cell_Inside_Returns_True()
    {
        var region = new Region(0, 2, 0, 2);
        Assert.True(region.Contains(0, 0));
        Assert.True(region.Contains(1, 1));
        Assert.True(region.Contains(2, 2));
    }

    [Test]
    public void Contains_Cell_Outside_Returns_False()
    {
        var region = new Region(0, 2, 0, 2);
        Assert.False(region.Contains(0, 5));
        Assert.False(region.Contains(10, 10));
        Assert.False(region.Contains(-1, 0));
    }

    [Test]
    public void Contains_Row_Col_Returns_Correctly()
    {
        var region = new Region(1, 5, 7, 12);
        Assert.True(region.SpansRow(3));
        Assert.False(region.SpansRow(8));
        Assert.True(region.SpansCol(8));
        Assert.False(region.SpansCol(3));
    }

    [Test]
    public void Copy_Region_Copies_Ok()
    {
        const int r0 = 5;
        const int r1 = 7;
        const int c0 = 2;
        const int c1 = 8;
        var region = new Region(r0, r1, c0, c1);
        var region_reverse = new Region(r1, r0, c1, c0);
        var copy = region.Copy() as Region;
        var copyReverse = region.CopyOrdered();

        Assert.AreEqual(r0, copy.TopLeft.Row);
        Assert.AreEqual(r1, copy.BottomRight.Row);
        Assert.AreEqual(c0, copy.TopLeft.Col);
        Assert.AreEqual(c1, copy.BottomRight.Col);

        Assert.AreEqual(r0, copyReverse.TopLeft.Row);
        Assert.AreEqual(r1, copyReverse.BottomRight.Row);
        Assert.AreEqual(c0, copyReverse.TopLeft.Col);
        Assert.AreEqual(c1, copyReverse.BottomRight.Col);
    }

    [Test]
    public void Get_Intersection_With_Region_Inside_Is_Same_As_Region_Inside()
    {
        var large = new Region(0, 3, 0, 3);
        var small = new Region(1, 2, 1, 2);
        var intersection = large.GetIntersection(small);
        var intersection2 = large.GetIntersection(small);
        Assert.AreEqual(small, intersection);
        Assert.AreEqual(intersection, intersection2);
    }

    [Test]
    public void Get_Intersection_With_Backwards_Region_Inside_Is_Same_As_Region_Inside()
    {
        var large = new Region(0, 3, 0, 3);
        var small = new Region(2, 1, 2, 1);
        var intersection = large.GetIntersection(small);
        var intersection2 = large.GetIntersection(small);
        Assert.AreEqual(small.CopyOrdered(), intersection);
        Assert.AreEqual(intersection, intersection2);
    }

    [Test]
    public void Get_Intersection_Outside_Returns_Null()
    {
        var r1 = new Region(0, 0);
        var r2 = new Region(1, 1);
        var i = r1.GetIntersection(r2);
        Assert.AreEqual(null, i);
    }

    [Test]
    public void Get_Intersection_Partial_Across_Is_Ok()
    {
        var r1 = new Region(0, 2, 0, 2);
        var r2 = new Region(1, 1, 2, 3);
        var i = r1.GetIntersection(r2);
        Assert.AreEqual(new Region(1, 1, 2, 2), i);
    }

    [Test]
    public void Column_Creation_Contains_Col_Correctly()
    {
        var region = new ColumnRegion(1, 3);
        Assert.True(region.Contains(100, 1));
        Assert.True(region.Contains(100, 2));
        Assert.True(region.Contains(100, 3));
    }

    [Test]
    public void Column_Creation_Backwards_Contains_Col_Correctly()
    {
        var region = new ColumnRegion(3, 1);
        Assert.True(region.Contains(100, 1));
        Assert.True(region.Contains(100, 2));
        Assert.True(region.Contains(100, 3));
    }

    [Test]
    public void Column_Intersection_With_Fixed_Region_Returns_Fixed_Region()
    {
        var fixedRegion = new Region(0, 2, 0, 2);
        var fixedRegion2 = new Region(0, 2, -1, 11);
        var colRegion = new ColumnRegion(0, 10);
        var intersection = colRegion.GetIntersection(fixedRegion);
        var intersection2 = colRegion.GetIntersection(fixedRegion2);
        Assert.AreEqual(fixedRegion, intersection);
        Assert.AreEqual(0, intersection2.TopLeft.Col);
        Assert.AreEqual(10, intersection2.BottomRight.Col);
    }

    [Test]
    // Cell on edge
    [TestCase(0, 0, 0, 0, 2, TestName = "String on top edge")]
    // Cell on bottom edge
    [TestCase(2, 2, 2, 2, 2, TestName = "String on bottom edge")]
    // Cell in middle
    [TestCase(1, 1, 1, 1, 4)]
    //Cell across region
    [TestCase(1, 1, -5, 5, 2)]
    [TestCase(-1, 1, -5, 5, 1)]
    [TestCase(10, 2, -5, 5, 1)]
    [TestCase(-10, 10, 1, 5, 1)]
    [TestCase(1, 1, 1, 5, 3)]
    public void Break_Region_Around_Regions_Correct(int r0, int r1, int c0, int c1, int nExpected)
    {
        var region = new Region(0, 2, 0, 2);
        var regionToBreakAround = new Region(r0, r1, c0, c1);

        var breaks = region.Break(regionToBreakAround);
        Assert.AreEqual(nExpected, breaks.Count());
        var totalArea = breaks.Sum(c => c.Area);
        Assert.AreEqual(region.Area - region.GetIntersection(regionToBreakAround).Area, totalArea);

        foreach (var breakRegion in breaks)
        {
            // None of the new regions should be overlapping with the break cell
            Assert.Null(breakRegion.GetIntersection(regionToBreakAround));
        }
    }

    [Test]
    public void Break_Around_Region_On_Left_Edge_Correct()
    {
        var region = new Region(1, 3, 1, 5);
        var breakRegion = new Region(1, 3, 1, 3);
        var expected = new Region(1, 3, 4, 5);
        Assert.AreEqual(expected, (region.Break(breakRegion)).First());
        //Assert.IsTrue(expected.Equals(region.Break(breakRegion)));
    }

    [Test]
    // Cell on edge
    [TestCase(0, 0, 0, 0, 2, TestName = "String on top edge")]
    // Cell on bottom edge
    [TestCase(2, 2, 2, 2, 2, TestName = "String on bottom edge")]
    // Cell in middle
    [TestCase(1, 1, 1, 1, 4)]
    //Cell across region
    [TestCase(1, 1, -5, 5, 2)]
    [TestCase(-1, 1, -5, 5, 1)]
    [TestCase(10, 2, -5, 5, 1)]
    [TestCase(-10, 10, 1, 5, 1)]
    [TestCase(1, 1, 1, 5, 3)]
    public void Break_Backwards_Region_Around_Regions_Correct(int r0, int r1, int c0, int c1, int nExpected)
    {
        var region = new Region(2, 0, 2, 0);
        var regionToBreakAround = new Region(r0, r1, c0, c1);

        var breaks = region.Break(regionToBreakAround);
        Assert.AreEqual(nExpected, breaks.Count());
        var totalArea = breaks.Sum(c => c.Area);
        Assert.AreEqual(region.Area - region.GetIntersection(regionToBreakAround).Area, totalArea);

        foreach (var breakRegion in breaks)
        {
            // None of the new regions should be overlapping with the break cell
            Assert.Null(breakRegion.GetIntersection(regionToBreakAround));
        }
    }

    [Test]
    public void Extend_Region_That_Changes_Dir_Works_Ok()
    {
        var region = new Region(1, 2, 1, 2);
        region.ExtendTo(0, 0);
        Assert.AreEqual(new CellPosition(0, 0), region.TopLeft);
        Assert.AreEqual(new CellPosition(1, 1), region.BottomRight);
    }

    [Test]
    public void Extend_To_Shrinks_To_New_Position()
    {
        var region = new Region(0, 5, 0, 5);
        region.ExtendTo(1, 2);
        Assert.AreEqual(1, region.BottomRight.Row);
        Assert.AreEqual(2, region.BottomRight.Col);
        Assert.AreEqual(0, region.TopLeft.Col);
        Assert.AreEqual(0, region.TopLeft.Col);
    }

    [Test]
    public void Get_Intersection_Preserves_Start_Posn_Of_Region_Acted_on()
    {
        var region1 = new Region(2, 0, 2, 0);
        var region2 = new Region(0, 10, 0, 10);
        var intersect1 = region1.GetIntersection(region2);
        var intersect2 = region2.GetIntersection(region1);
        Assert.AreEqual(region1.TopLeft, intersect1.TopLeft);
        Assert.AreEqual(region2.TopLeft, intersect2.TopLeft);
    }

    [Test]
    public void Extend_Column_Extends_To_Col()
    {
        var r1 = new ColumnRegion(1, 1);
        r1.ExtendTo(0, 3);
        Assert.AreEqual(3, r1.BottomRight.Col);
        Assert.AreEqual(1, r1.TopLeft.Col);
        r1.ExtendTo(323, 0);
        Assert.AreEqual(1, r1.BottomRight.Col);
        Assert.AreEqual(0, r1.TopLeft.Col);
    }

    [Test]
    public void Extend_Row_Extends_To_Row()
    {
        var r1 = new RowRegion(1, 1);
        r1.ExtendTo(3, 0);
        Assert.AreEqual(3, r1.BottomRight.Row);
        Assert.AreEqual(1, r1.TopLeft.Row);
        r1.ExtendTo(0, 0);
        Assert.AreEqual(1, r1.BottomRight.Row);
        Assert.AreEqual(0, r1.TopLeft.Row);
    }
}