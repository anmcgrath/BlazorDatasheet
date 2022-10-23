using System;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class SheetTests
{
    [Test]
    public void Create_Sheet_2x1_Has_Empty_Cells()
    {
        var sheet = new Sheet(2, 1);
        Assert.AreEqual(2, sheet.Rows.Count);
        Assert.AreEqual(1, sheet.Rows[0].Cells.Count);
        Assert.AreEqual(1, sheet.Rows[1].Cells.Count);

        Assert.AreEqual(null, sheet.GetCell(0, 0).GetValue());
        Assert.AreEqual(null, sheet.GetCell(1, 0).GetValue());
    }

    [Test]
    public void Add_Row_Adds_Row_And_Updates_Numbers()
    {
        var sheet = new Sheet(3, 1);
        sheet.InsertRowAt(1);
        Assert.AreEqual(4, sheet.NumRows);
        Assert.AreEqual(4, sheet.Rows.Count);
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            Assert.AreEqual(i, sheet.Rows[i].RowNumber);
            foreach (var cell in sheet.Rows[i].Cells)
            {
                Assert.AreEqual(i, cell.Row);
            }
        }
    }

    [Test]
    public void Add_Row_To_End_Adds_Row_And_Updates_Numbers()
    {
        bool eventFired = false;
        var sheet = new Sheet(3, 1);
        sheet.RowInserted += (sender, args) => eventFired = true;
        sheet.InsertRow();
        Assert.AreEqual(4, sheet.NumRows);
        Assert.AreEqual(4, sheet.Rows.Count);
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            Assert.AreEqual(i, sheet.Rows[i].RowNumber);
            foreach (var cell in sheet.Rows[i].Cells)
            {
                Assert.AreEqual(i, cell.Row);
            }
        }

        Assert.IsTrue(eventFired);
    }

    [Test]
    [TestCase(0,1,0,1)]
    [TestCase(0,1,0,0)]
    [TestCase(1,2,1,1)]
    public void Get_delim_Data_from_Sheet(int copyPasteRegionR0, int copyPasteRegionR1, int copyPasteRegionC0, int copyPasteRegionC1)
    {
        var sheet = new Sheet(5, 5);
        var copyPasteRegion = new Region(copyPasteRegionR0, copyPasteRegionR1, copyPasteRegionC0, copyPasteRegionC1);

        foreach (var posn in copyPasteRegion)
            sheet.TrySetCellValue(posn.Row, posn.Col, getCellPosnString(posn.Row, posn.Col));

        var copy = sheet.GetRegionAsDelimitedText(copyPasteRegion);
        Assert.NotNull(copy);
        Assert.AreNotEqual(String.Empty, copy);

        // Clear the sheet so we are pasting over empty data
        sheet.ClearCells(new[] { copyPasteRegion });

        var insertedRegions = sheet.InsertDelimitedText(copy, copyPasteRegion.TopLeft);
        
        Assert.True(insertedRegions.Equals(copyPasteRegion));
        
        foreach (var posn in copyPasteRegion)
            Assert.AreEqual(getCellPosnString(posn.Row, posn.Col),
                sheet.GetCell(posn.Row, posn.Col).GetValue<string>());
    }

    private string getCellPosnString(int row, int col)
    {
        return $"({row},{col})";
    }
}