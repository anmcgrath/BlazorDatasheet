using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class InsertColAfterCommandTests
{
    [Test]
    public void Insert_Col_Then_Undo_Works_Ok()
    {
        var sheet = new Sheet(1, 3);
        sheet.TrySetCellValue(0, 0, "0,0");
        sheet.TrySetCellValue(0, 2, "0,2");
        sheet.InsertColAfter(0);

        Assert.AreEqual(4, sheet.NumCols);
        Assert.AreEqual("0,0", sheet.GetValue(0, 0));
        Assert.AreEqual("0,2", sheet.GetValue(0, 3));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumCols);
        Assert.AreEqual("0,0", sheet.GetValue(0, 0));
        Assert.AreEqual("0,2", sheet.GetValue(0, 2));
    }
}