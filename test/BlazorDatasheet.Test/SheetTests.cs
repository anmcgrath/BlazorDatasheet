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
}