using BlazorDatasheet.Data;
using NUnit.Framework;
using BlazorDatasheet.Commands;

namespace BlazorDatasheet.Test.Commands;

public class InsertRowAfterCommandTests
{
    [Test]
    public void Insert_Row_Then_Undo_Correct()
    {
        var sheet = new Sheet(3, 1);
        sheet.TrySetCellValue(0, 0, "0,0");
        sheet.TrySetCellValue(2, 0, "2,0");
        sheet.InsertRowAfter(0);

        Assert.AreEqual(4, sheet.NumRows);
        Assert.AreEqual("0,0", sheet.GetValue(0, 0));
        Assert.AreEqual("2,0", sheet.GetValue(3, 0));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumRows);
        Assert.AreEqual("0,0", sheet.GetValue(0, 0));
        Assert.AreEqual("2,0", sheet.GetValue(2, 0));
    }
}