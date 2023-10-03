using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class MetaDataTests
{
    [Test]
    public void Set_Cell_MetaData_And_Undo_Works()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetCellMetaData(1, 1, "test", 7);
        Assert.AreEqual(7, sheet.GetMetaData(1, 1, "test"));
        sheet.SetCellMetaData(1, 1, "test", 8);
        Assert.AreEqual(8, sheet.GetMetaData(1, 1, "test"));
        sheet.Commands.Undo();
        Assert.AreEqual(7, sheet.GetMetaData(1, 1, "test"));
    }

    [Test]
    public void Set_Cell_MetaData_Fires_Event()
    {
        var sheet = new Sheet(3, 3);
        var fired = false;
        var rowFired = -1;
        var colFired = -1;
        var nameFired = "";
        object? newData = null;
        sheet.MetaDataChanged += (sender, args) =>
        {
            fired = true;
            rowFired = args.Row;
            colFired = args.Col;
            newData = args.NewValue;
            nameFired = args.Name;
        };

        sheet.SetCellMetaData(1, 2, "test", "value");

        Assert.True(fired);
        Assert.AreEqual(1, rowFired);
        Assert.AreEqual(2, colFired);
        Assert.AreEqual("test", nameFired);
        Assert.AreEqual("value", newData);
    }
}