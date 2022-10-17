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
        var sheet = new Sheet(3, 1);
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
    }
}