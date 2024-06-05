using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Geometry;

public class RegionTests
{
    [Test]
    public void Create_Region_With_Single_Cell_Size_Sets_Correct_Row_Cols()
    {
        // Create a region at row = 10, col = 11 with size 1
        var region = new Region(10, 11);
        Assert.AreEqual(10, region.TopLeft.row);
        Assert.AreEqual(10, region.BottomRight.row);
        Assert.AreEqual(11, region.TopLeft.col);
        Assert.AreEqual(11, region.BottomRight.col);
    }

    [Test]
    public void Create_Region_With_Specific_Starts_And_Ends_Creates_With_Correct_Row_Cols()
    {
        var region = new Region(1, 2, 3, 4);
        Assert.AreEqual(1, region.TopLeft.row);
        Assert.AreEqual(2, region.BottomRight.row);
        Assert.AreEqual(3, region.TopLeft.col);
        Assert.AreEqual(4, region.BottomRight.col);
    }

    [Test]
    public void Constrain_To_Smaller_Region_Constrains_Correctly()
    {
        var large = new Region(0, 5, 1, 6);
        var small = new Region(2, 4, 3, 5);
        large.Constrain(small);
        Assert.AreEqual(small.TopLeft.row, large.TopLeft.row);
        Assert.AreEqual(small.BottomRight.row, large.BottomRight.row);
        Assert.AreEqual(small.TopLeft.col, large.TopLeft.col);
        Assert.AreEqual(small.BottomRight.col, large.BottomRight.col);
    }

    [Test]
    public void Constrain_To_Larger_Region_Constrains_Correctly()
    {
        var large = new Region(0, 5, 1, 6);
        var small = new Region(2, 4, 3, 5);
        small.Constrain(large);
        Assert.AreEqual(2, small.TopLeft.row);
        Assert.AreEqual(4, small.BottomRight.row);
        Assert.AreEqual(3, small.TopLeft.col);
        Assert.AreEqual(5, small.BottomRight.col);
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
        var copy = region.Copy() as Region;
        var copyReverse = region.CopyOrdered();

        Assert.AreEqual(r0, copy.TopLeft.row);
        Assert.AreEqual(r1, copy.BottomRight.row);
        Assert.AreEqual(c0, copy.TopLeft.col);
        Assert.AreEqual(c1, copy.BottomRight.col);

        Assert.AreEqual(r0, copyReverse.TopLeft.row);
        Assert.AreEqual(r1, copyReverse.BottomRight.row);
        Assert.AreEqual(c0, copyReverse.TopLeft.col);
        Assert.AreEqual(c1, copyReverse.BottomRight.col);
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
    public void GetIntersectionRowColRegionReturnsSingleCell()
    {
        var colRegion = new ColumnRegion(5);
        var rowREgion = new RowRegion(6);
        var colIntRow = colRegion.GetIntersection(rowREgion);
        colIntRow.Left.Should().Be(5);
        colIntRow.Top.Should().Be(6);
        colIntRow.Width.Should().Be(1);
        colIntRow.Height.Should().Be(1);
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
        Assert.AreEqual(0, intersection2.TopLeft.col);
        Assert.AreEqual(10, intersection2.BottomRight.col);
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
        /*
               0  1  2  3  4  5
           0 | x| x| x|  |  |  |
           1 | x| x| x|  |  |  |
           2 | x| x| x|  |  |  |
           3 |  |  |  |  |  |  |
           4 |  |  |  |  |  |  |
           5 |  |  |  |  |  |  |

         */
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
        Assert.AreEqual(1, region.BottomRight.row);
        Assert.AreEqual(2, region.BottomRight.col);
        Assert.AreEqual(0, region.TopLeft.col);
        Assert.AreEqual(0, region.TopLeft.col);
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
        Assert.AreEqual(3, r1.BottomRight.col);
        Assert.AreEqual(1, r1.TopLeft.col);
        r1.ExtendTo(323, 0);
        Assert.AreEqual(1, r1.BottomRight.col);
        Assert.AreEqual(0, r1.TopLeft.col);
    }

    [Test]
    public void Extend_Row_Extends_To_Row()
    {
        var r1 = new RowRegion(1, 1);
        r1.ExtendTo(3, 0);
        Assert.AreEqual(3, r1.BottomRight.row);
        Assert.AreEqual(1, r1.TopLeft.row);
        r1.ExtendTo(0, 0);
        Assert.AreEqual(1, r1.BottomRight.row);
        Assert.AreEqual(0, r1.TopLeft.row);
    }

    [Test]
    [TestCase(Edge.Left, 2, 2, 0, 0, 0)]
    [TestCase(Edge.Right, 2, 0, 2, 0, 0)]
    [TestCase(Edge.Top, 2, 0, 0, 2, 0)]
    [TestCase(Edge.Bottom, 2, 0, 0, 0, 2)]
    [TestCase(Edge.Left | Edge.Right, 2, 2, 2, 0, 0)]
    [TestCase(Edge.Top | Edge.Bottom, 2, 0, 0, 2, 2)]
    [TestCase(Edge.Top | Edge.Right | Edge.Bottom | Edge.Left, 2, 2, 2, 2, 2)]
    public void Expand_General_Region_Expands(Edge edges, int amount, int expectedDLeft, int expectedDRight,
        int expectedDTop, int expectedDBottom)
    {
        var l = 1;
        var r = 2;
        var t = 1;
        var b = 2;
        var region = new Region(t, b, l, r);
        region.Expand(edges, amount);
        Assert.AreEqual(expectedDLeft, l - region.Left);
        Assert.AreEqual(expectedDRight, region.Right - r);
        Assert.AreEqual(expectedDTop, t - region.Top);
        Assert.AreEqual(expectedDBottom, region.Bottom - b);
    }

    [Test]
    public void Expand_Single_Cell_Top_And_Left_Moves_Start_not_End()
    {
        var r = new Region(1, 1);
        r.Expand(Edge.Left | Edge.Top, 1);
        Assert.AreEqual(0, r.Start.col);
        Assert.AreEqual(0, r.Start.row);
        Assert.AreEqual(1, r.End.col);
        Assert.AreEqual(1, r.End.row);
    }

    [Test]
    public void Expand_Single_Cell_Right_And_Bottom_Moves_End_Not_Start()
    {
        var r = new Region(1, 1);
        r.Expand(Edge.Right | Edge.Bottom, 1);
        Assert.AreEqual(1, r.Start.col);
        Assert.AreEqual(1, r.Start.row);
        Assert.AreEqual(2, r.End.col);
        Assert.AreEqual(2, r.End.row);
    }

    [Test]
    public void Expand_Col_Region_By_All_Edges_Ok()
    {
        var c = new ColumnRegion(2);
        c.Expand(Edge.Bottom | Edge.Left | Edge.Right | Edge.Top, 2);
        Assert.AreEqual(c.Left, 0);
        Assert.AreEqual(c.Right, 4);
        Assert.AreEqual(c.Top, 0);
    }


    [Test]
    public void Expand_Row_Region_By_All_Edges_Ok()
    {
        var c = new RowRegion(2);
        c.Expand(Edge.Bottom | Edge.Left | Edge.Right | Edge.Top, 2);
        Assert.AreEqual(c.Top, 0);
        Assert.AreEqual(c.Bottom, 4);
        Assert.AreEqual(c.Left, 0);
    }
}