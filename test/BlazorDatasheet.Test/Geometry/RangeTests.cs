using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Geometry;

public class RangeTests
{
    [Test]
    public void Set_Values_Using_Range_Sets_Values()
    {
        var sheet = new Sheet(5, 5);
        var range = sheet.Range(new Region(0, 2, 0, 2));
        range.Value = "Test";
        Assert.AreEqual("Test", sheet.Cells.GetValue(0, 0));
        Assert.Null(sheet.Cells.GetValue(4, 4));
    }

    [Test]
    public void Positions_In_Ranges_Correct()
    {
        var nRows = 3;
        var nCols = 3;
        var sheet = new Sheet(nRows, nCols);
        var sheetRange = sheet.Range(sheet.Region);
        var posns = sheetRange.Positions.ToList();
        var cells = posns.Select(x => sheet.Cells.GetCell(x.row, x.col)).ToList();
        Assert.AreEqual(posns.Count, nRows * nCols);
        int i = 0;
        for (int row = 0; row < nRows; row++)
        {
            for (int col = 0; col < nCols; col++)
            {
                var p = posns[i];
                Assert.AreEqual(row, p.row);
                Assert.AreEqual(col, p.col);
                Assert.AreEqual(row, cells[i].Row);
                Assert.AreEqual(col, cells[i].Col);
                i++;
            }
        }
    }

    [Test]
    public void Get_Col_Row_Range_From_Sheet_Ok()
    {
        var sheet = new Sheet(3, 3);
        var rowRange = sheet.Range(Axis.Row, 1, 3);
        Assert.AreEqual(typeof(RowRegion), rowRange.Region.GetType());
        Assert.AreEqual(1, rowRange.Region.Start.row);
        Assert.AreEqual(3, rowRange.Region.End.row);

        var colRange = sheet.Range(Axis.Col, 1, 3);
        Assert.AreEqual(typeof(ColumnRegion), colRange.Region.GetType());
        Assert.AreEqual(1, colRange.Region.Start.col);
        Assert.AreEqual(3, colRange.Region.End.col);
    }

    [Test]
    [TestCase(0, 0, 0, 0, false, false, false, false, "A1")]
    [TestCase(1, 1, 2, 2, false, false, false, false, "C2")]
    [TestCase(0, 0, 0, 0, false, false, true, true, "$A1")]
    [TestCase(0, 0, 0, 0, true, true, false, false, "A$1")]
    [TestCase(0, 0, 0, 0, true, true, true, true, "$A$1")]
    [TestCase(1, 2, 1, 2, false, false, false, false, "B2:C3")]
    [TestCase(1, 2, 2, 2, false, false, false, false, "C2:C3")]
    [TestCase(1, 2, 2, 2, true, false, false, false, "C$2:C3")]
    [TestCase(1, 2, 2, 2, false, true, false, false, "C2:C$3")]
    [TestCase(1, 2, 2, 2, false, false, true, false, "$C2:C3")]
    [TestCase(1, 2, 2, 2, false, false, false, true, "C2:$C3")]
    public void Range_Text_Tests(int r0, int r1, int c0, int c1, bool row0Fixed, bool row1Fixed, bool col0Fixed,
        bool col1Fixed,
        string expected)
    {
        var region = new Region(r0, r1, c0, c1);
        RangeText.ToRegionText(region, col0Fixed, col1Fixed, row0Fixed, row1Fixed)
            .Should()
            .Be(expected);
    }
}