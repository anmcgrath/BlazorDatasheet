using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class RangeTests
{
    [Test]
    public void Create_Range_With_Single_Cell_Size_Sets_Correct_Row_Cols()
    {
        // Create a range at row = 10, col = 11 with size 1
        var range = new Range(10, 11);
        Assert.AreEqual(10, range.Start.Row);
        Assert.AreEqual(10, range.End.Row);
        Assert.AreEqual(11, range.Start.Col);
        Assert.AreEqual(11, range.End.Col);
    }

    [Test]
    public void Create_Range_With_Specific_Starts_And_Ends_Creates_With_Correct_Row_Cols()
    {
        var range = new Range(1, 2, 3, 4);
        Assert.AreEqual(1, range.Start.Row);
        Assert.AreEqual(2, range.End.Row);
        Assert.AreEqual(3, range.Start.Col);
        Assert.AreEqual(4, range.End.Col);
    }

    [Test]
    public void Create_Backwards_Range_With_Specific_Starts_And_Ends_Creates_With_Correct_Row_Cols()
    {
        var range = new Range(2, 1, 4, 3);
        Assert.AreEqual(2, range.Start.Row);
        Assert.AreEqual(1, range.End.Row);
        Assert.AreEqual(4, range.Start.Col);
        Assert.AreEqual(3, range.End.Col);
    }

    [Test]
    public void Constrain_To_Smaller_Range_Constrains_Correctly()
    {
        var large = new Range(0, 5, 1, 6);
        var small = new Range(2, 4, 3, 5);
        large.Constrain(small);
        Assert.AreEqual(small.Start.Row, large.Start.Row);
        Assert.AreEqual(small.End.Row, large.End.Row);
        Assert.AreEqual(small.Start.Col, large.Start.Col);
        Assert.AreEqual(small.End.Col, large.End.Col);
    }

    [Test]
    public void Constrain_To_Larger_Range_Constrains_Correctly()
    {
        var large = new Range(0, 5, 1, 6);
        var small = new Range(2, 4, 3, 5);
        small.Constrain(large);
        Assert.AreEqual(2, small.Start.Row);
        Assert.AreEqual(4, small.End.Row);
        Assert.AreEqual(3, small.Start.Col);
        Assert.AreEqual(5, small.End.Col);
    }

    [Test]
    public void Enumerate_Range_Moves_In_Row_Dir()
    {
        var range = new Range(0, 2, 0, 2);
        var posns = range.ToList();
        Assert.AreEqual(range.Area, posns.Count);
        // Check first cell (top left)
        Assert.AreEqual(0, posns[0].Row);
        Assert.AreEqual(0, posns[0].Col);
        // Check end of first row
        Assert.AreEqual(0, posns[2].Row);
        Assert.AreEqual(2, posns[2].Col);
        // Check first cell in second row
        Assert.AreEqual(1, posns[3].Row);
        Assert.AreEqual(0, posns[3].Col);
    }

    [Test]
    public void Enumerate_Backwards_Range_Moves_In_Reverse_Dir()
    {
        var range = new Range(2, 0, 2, 0);
        var posns = range.ToList();
        Assert.AreEqual(range.Area, posns.Count);
        // Check first cell (top left)
        Assert.AreEqual(2, posns[0].Row);
        Assert.AreEqual(2, posns[0].Col);
        // Check end of first row
        Assert.AreEqual(2, posns[2].Row);
        Assert.AreEqual(0, posns[2].Col);
        // Check first cell in second row
        Assert.AreEqual(1, posns[3].Row);
        Assert.AreEqual(2, posns[3].Col);
    }

    [Test]
    public void Width_Height_Area_Ok()
    {
        var range = new Range(1, 4, 2, 7);
        Assert.AreEqual(4, range.Height);
        Assert.AreEqual(6, range.Width);
        Assert.AreEqual(24, range.Area);
    }

    [Test]
    public void Backwards_Range_Height_Width_Ok()
    {
        var range = new Range(4, 1, 7, 2);
        Assert.AreEqual(4, range.Height);
        Assert.AreEqual(6, range.Width);
        Assert.AreEqual(24, range.Area);
    }

    [Test]
    public void Contains_Cell_Inside_Returns_True()
    {
        var range = new Range(0, 2, 0, 2);
        Assert.True(range.Contains(0, 0));
        Assert.True(range.Contains(1, 1));
        Assert.True(range.Contains(2, 2));
    }

    [Test]
    public void Contains_Cell_Outside_Returns_False()
    {
        var range = new Range(0, 2, 0, 2);
        Assert.False(range.Contains(0, 5));
        Assert.False(range.Contains(10, 10));
        Assert.False(range.Contains(-1, 0));
    }

    [Test]
    public void Contains_Row_Col_Returns_Correctly()
    {
        var range = new Range(1, 5, 7, 12);
        Assert.True(range.SpansRow(3));
        Assert.False(range.SpansRow(8));
        Assert.True(range.SpansCol(8));
        Assert.False(range.SpansCol(3));
    }

    [Test]
    public void Copy_Range_Copies_Ok()
    {
        const int r0 = 5;
        const int r1 = 7;
        const int c0 = 2;
        const int c1 = 8;
        var range = new Range(r0, r1, c0, c1);
        var range_reverse = new Range(r1, r0, c1, c0);
        var copy = range.Copy() as Range;
        var copyReverse = range.CopyOrdered();

        Assert.AreEqual(r0, copy.Start.Row);
        Assert.AreEqual(r1, copy.End.Row);
        Assert.AreEqual(c0, copy.Start.Col);
        Assert.AreEqual(c1, copy.End.Col);

        Assert.AreEqual(r0, copyReverse.Start.Row);
        Assert.AreEqual(r1, copyReverse.End.Row);
        Assert.AreEqual(c0, copyReverse.Start.Col);
        Assert.AreEqual(c1, copyReverse.End.Col);
    }

    [Test]
    public void Get_Intersection_With_Range_Inside_Is_Same_As_Range_Inside()
    {
        var large = new Range(0, 3, 0, 3);
        var small = new Range(1, 2, 1, 2);
        var intersection = large.GetIntersection(small);
        var intersection2 = large.GetIntersection(small);
        Assert.AreEqual(small, intersection);
        Assert.AreEqual(intersection, intersection2);
    }

    [Test]
    public void Get_Intersection_With_Backwards_Range_Inside_Is_Same_As_Range_Inside()
    {
        var large = new Range(0, 3, 0, 3);
        var small = new Range(2, 1, 2, 1);
        var intersection = large.GetIntersection(small);
        var intersection2 = large.GetIntersection(small);
        Assert.AreEqual(small.CopyOrdered(), intersection);
        Assert.AreEqual(intersection, intersection2);
    }

    [Test]
    public void Get_Intersection_Outside_Returns_Null()
    {
        var r1 = new Range(0, 0);
        var r2 = new Range(1, 1);
        var i = r1.GetIntersection(r2);
        Assert.AreEqual(null, i);
    }

    [Test]
    public void Get_Intersection_Partial_Across_Is_Ok()
    {
        var r1 = new Range(0, 2, 0, 2);
        var r2 = new Range(1, 1, 2, 3);
        var i = r1.GetIntersection(r2);
        Assert.AreEqual(new Range(1, 1, 2, 2), i);
    }

    [Test]
    public void Column_Creation_Contains_Col_Correctly()
    {
        var range = new ColumnRange(1, 3);
        Assert.True(range.Contains(100, 1));
        Assert.True(range.Contains(100, 2));
        Assert.True(range.Contains(100, 3));
    }

    [Test]
    public void Column_Creation_Backwards_Contains_Col_Correctly()
    {
        var range = new ColumnRange(3, 1);
        Assert.True(range.Contains(100, 1));
        Assert.True(range.Contains(100, 2));
        Assert.True(range.Contains(100, 3));
    }

    [Test]
    public void Column_Intersection_With_Fixed_Range_Returns_Fixed_Range()
    {
        var fixedRange = new Range(0, 2, 0, 2);
        var fixedRange2 = new Range(0, 2, -1, 11);
        var colRange = new ColumnRange(0, 10);
        var intersection = colRange.GetIntersection(fixedRange);
        var intersection2 = colRange.GetIntersection(fixedRange2);
        Assert.AreEqual(fixedRange, intersection);
        Assert.AreEqual(0, intersection2.Start.Col);
        Assert.AreEqual(10, intersection2.End.Col);
    }

    [Test]
    // Cell on edge
    [TestCase(0, 0, 0, 0, 2, TestName = "String on top edge")]
    // Cell on bottom edge
    [TestCase(2, 2, 2, 2, 2, TestName = "String on bottom edge")]
    // Cell in middle
    [TestCase(1, 1, 1, 1, 4)]
    //Cell across range
    [TestCase(1, 1, -5, 5, 2)]
    [TestCase(-1, 1, -5, 5, 1)]
    [TestCase(10, 2, -5, 5, 1)]
    [TestCase(-10, 10, 1, 5, 1)]
    [TestCase(1, 1, 1, 5, 3)]
    public void Break_Range_Around_Ranges_Correct(int r0, int r1, int c0, int c1, int nExpected)
    {
        var range = new Range(0, 2, 0, 2);
        var rangeToBreakAround = new Range(r0, r1, c0, c1);

        var breaks = range.Break(rangeToBreakAround);
        Assert.AreEqual(nExpected, breaks.Count());
        var totalArea = breaks.Sum(c => c.Area);
        Assert.AreEqual(range.Area - range.GetIntersection(rangeToBreakAround).Area, totalArea);

        foreach (var breakRange in breaks)
        {
            // None of the new ranges should be overlapping with the break cell
            Assert.Null(breakRange.GetIntersection(rangeToBreakAround));
        }
    }

    [Test]
    // Cell on edge
    [TestCase(0, 0, 0, 0, 2, TestName = "String on top edge")]
    // Cell on bottom edge
    [TestCase(2, 2, 2, 2, 2, TestName = "String on bottom edge")]
    // Cell in middle
    [TestCase(1, 1, 1, 1, 4)]
    //Cell across range
    [TestCase(1, 1, -5, 5, 2)]
    [TestCase(-1, 1, -5, 5, 1)]
    [TestCase(10, 2, -5, 5, 1)]
    [TestCase(-10, 10, 1, 5, 1)]
    [TestCase(1, 1, 1, 5, 3)]
    public void Break_Backwards_Range_Around_Ranges_Correct(int r0, int r1, int c0, int c1, int nExpected)
    {
        var range = new Range(2, 0, 2, 0);
        var rangeToBreakAround = new Range(r0, r1, c0, c1);

        var breaks = range.Break(rangeToBreakAround);
        Assert.AreEqual(nExpected, breaks.Count());
        var totalArea = breaks.Sum(c => c.Area);
        Assert.AreEqual(range.Area - range.GetIntersection(rangeToBreakAround).Area, totalArea);

        foreach (var breakRange in breaks)
        {
            // None of the new ranges should be overlapping with the break cell
            Assert.Null(breakRange.GetIntersection(rangeToBreakAround));
        }
    }

    [Test]
    public void Extend_Range_That_Changes_Dir_Works_Ok()
    {
        var range = new Range(1, 2, 1, 2);
        range.ExtendTo(0, 0);
        Assert.AreEqual(new CellPosition(0, 0), range.End);
        Assert.AreEqual(new CellPosition(1, 1), range.Start);
    }

    [Test]
    public void Get_Intersection_Preserves_Start_Posn_Of_Range_Acted_on()
    {
        var range1 = new Range(2, 0, 2, 0);
        var range2 = new Range(0, 10, 0, 10);
        var intersect1 = range1.GetIntersection(range2);
        var intersect2 = range2.GetIntersection(range1);
        Assert.AreEqual(range1.Start, intersect1.Start);
        Assert.AreEqual(range2.Start, intersect2.Start);
    }

    [Test]
    public void Set_Reverse_Order_Updates_Positions()
    {
        var range = new Range(0, 1, 0, 1);
        range.SetOrder(-1, -1);
        Assert.AreEqual(new CellPosition(1, 1), range.Start);
        Assert.AreEqual(new CellPosition(0, 0), range.End);
    }

    [Test]
    public void Set_Same_Order_For_Reversed_Range_Keeps_Order()
    {
        var range = new Range(5, 1, 1, 1);
        var copy = range.Copy() as Range;
        copy.SetOrder(range.RowDir, range.ColDir);
        Assert.AreEqual(copy, range);
    }
    
    [Test]
    public void Set_Same_Order_For_Range_Keeps_Order()
    {
        var range = new Range(1, 5, 1, 1);
        var copy = range.Copy() as Range;
        copy.SetOrder(range.RowDir, range.ColDir);
        Assert.AreEqual(copy, range);
    }

    [Test]
    public void Set_Order_Then_Extend_Extends_Correctly()
    {
        var range = new Range(1,1,1,1);
        var rangeLarger = new Range(0, 10, 0, 10);
        var intersect = range.GetIntersection(rangeLarger);
        Assert.AreEqual(range, intersect);
        intersect.ExtendTo(1, 2);
        intersect = rangeLarger.GetIntersection(intersect);
        Assert.AreEqual(range.Start, intersect.Start);
        Assert.AreEqual(new CellPosition(1, 2), intersect.End);
    }
}