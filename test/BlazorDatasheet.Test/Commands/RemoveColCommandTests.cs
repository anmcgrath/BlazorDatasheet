using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class RemoveColCommandTests
{
    [Test]
    public void Remove_Col_Then_Undo_Works_Ok()
    {
        var sheet = new Sheet(1, 3);
        sheet.TrySetCellValue(0, 2, "0,2");
        sheet.TrySetCellValue(0, 3, "0,3");
        sheet.RemoveCol(2);

        Assert.AreEqual(2, sheet.NumCols);
        Assert.AreEqual("0,3", sheet.GetValue(0,2));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumCols);
        Assert.AreEqual("0,2", sheet.GetValue(0,2));
        Assert.AreEqual("0,3", sheet.GetValue(0,3));
    }
}