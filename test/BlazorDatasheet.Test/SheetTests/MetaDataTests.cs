using BlazorDatasheet.Core.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class MetaDataTests
{
    [Test]
    public void Set_Cell_MetaData_And_Undo_Works()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        Assert.AreEqual(7, sheet.Cells.GetMetaData(1, 1, "test"));
        sheet.Cells.SetCellMetaData(1, 1, "test", 8);
        Assert.AreEqual(8, sheet.Cells.GetMetaData(1, 1, "test"));
        sheet.Commands.Undo();
        Assert.AreEqual(7, sheet.Cells.GetMetaData(1, 1, "test"));
    }
}