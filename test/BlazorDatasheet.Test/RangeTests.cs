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
        Assert.AreEqual(10, range.RowStart);
        Assert.AreEqual(10, range.RowEnd);
        Assert.AreEqual(11, range.ColStart);
        Assert.AreEqual(11, range.ColEnd);
    }
    [Test]
    public void Create_Range_With_Specific_Starts_And_Ends_Creates_With_Correct_Row_Cols()
    {
        var range = new Range(1, 2, 3, 4);
        Assert.AreEqual(1, range.RowStart);
        Assert.AreEqual(2, range.RowEnd);
        Assert.AreEqual(3, range.ColStart);
        Assert.AreEqual(4, range.ColEnd);
    }
    
    [Test]
    public void Create_Backwards_Range_With_Specific_Starts_And_Ends_Creates_With_Correct_Row_Cols()
    {
        var range = new Range(2, 1, 4, 3);
        Assert.AreEqual(2, range.RowStart);
        Assert.AreEqual(1, range.RowEnd);
        Assert.AreEqual(4, range.ColStart);
        Assert.AreEqual(3, range.ColEnd);
    }

    [Test]
    public void Constrain_To_Smaller_Range_Constrains_Correctly()
    {
        var large = new Range(0, 5, 1, 6);
        var small = new Range(2, 4, 3, 5);
        large.Constrain(small);
        Assert.AreEqual(small.RowStart, large.RowStart);
        Assert.AreEqual(small.RowEnd, large.RowEnd);
        Assert.AreEqual(small.ColStart, large.ColStart);
        Assert.AreEqual(small.ColEnd, large.ColEnd);
    }

    [Test]
    public void Constrain_Using_Width_Height_Constrains_Correctly()
    {
        var range = new Range(-1, 3, -1, 3);
        range.Constrain(2, 2);
        Assert.AreEqual(2, range.Width);
        Assert.AreEqual(2, range.Height);
        Assert.AreEqual(0, range.RowStart);
        Assert.AreEqual(1, range.RowEnd);
        Assert.AreEqual(0, range.ColStart);
        Assert.AreEqual(1, range.ColEnd);
    }

    [Test]
    public void Constrain_To_Larger_Range_Constrains_Correctly()
    {
        var large = new Range(0, 5, 1, 6);
        var small = new Range(2, 4, 3, 5);
        small.Constrain(large);
        Assert.AreEqual(2, small.RowStart);
        Assert.AreEqual(4, small.RowEnd);
        Assert.AreEqual(3, small.ColStart);
        Assert.AreEqual(5, small.ColEnd);
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
    public void Width_Height_Area_Ok()
    {
        var range = new Range(1, 4, 2, 7);
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
        Assert.True(range.ContainsRow(3));
        Assert.False(range.ContainsRow(8));
        Assert.True(range.ContainsCol(8));
        Assert.False(range.ContainsCol(3));
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
        var copy = range.Copy();
        var copyReverse = range.CopyOrdered();

        Assert.AreEqual(r0, copy.RowStart);
        Assert.AreEqual(r1, copy.RowEnd);
        Assert.AreEqual(c0, copy.ColStart);
        Assert.AreEqual(c1, copy.ColEnd);
        
        Assert.AreEqual(r0, copyReverse.RowStart);
        Assert.AreEqual(r1, copyReverse.RowEnd);
        Assert.AreEqual(c0, copyReverse.ColStart);
        Assert.AreEqual(c1, copyReverse.ColEnd);
    }
}