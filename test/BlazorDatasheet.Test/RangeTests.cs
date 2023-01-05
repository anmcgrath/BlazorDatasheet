using System.Linq;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class RangeTests
{
    [Test]
    public void Set_Values_Using_Range_Sets_Values()
    {
        var sheet = new Sheet(5, 5);
        var range = sheet.Range(new Region(0, 2, 0, 2));
        range.Value = "Test";
        Assert.AreEqual("Test", sheet.GetCellValue(0, 0));
        Assert.Null(sheet.GetCellValue(4, 4));
    }

    [Test]
    public void Positions_In_Ranges_Correct()
    {
        var nRows = 3;
        var nCols = 3;
        var sheet = new Sheet(nRows, nCols);
        var sheetRange = sheet.Range(sheet.Region);
        var posns = sheetRange.Positions.ToList();
        var cells = sheetRange.GetCells().ToList();
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
    public void BRange_Cell_Sets_Value_Correctly()
    {
        var sheet = new Sheet(2, 2);
        var cell = sheet.Range(0, 0);
        cell.Value = "Test";
        Assert.AreEqual(cell.Value, "Test");
        Assert.AreEqual(sheet.GetCellValue(0, 0), "Test");
    }
}