using BlazorDatasheet.Data;
using NUnit.Framework;
using BlazorDatasheet.Commands;

namespace BlazorDatasheet.Test.Commands;

public class InsertRowAtCommandTests
{
    [Test]
    public void Insert_Row_Then_Undo_Correct()
    {
        var sheet = new Sheet(3, 1);
        sheet.SetCellValue(0, 0, "0,0");
        sheet.SetCellValue(2, 0, "2,0");
        sheet.InsertRowAt(0);

        Assert.AreEqual(4, sheet.NumRows);
        Assert.AreEqual("0,0", sheet.GetValue(1, 0));
        Assert.AreEqual("2,0", sheet.GetValue(3, 0));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumRows);
        Assert.AreEqual("0,0", sheet.GetValue(0, 0));
        Assert.AreEqual("2,0", sheet.GetValue(2, 0));
    }

    [Test]
    public void Insert_Row_After_End_Of_Sheet_Appends_Row_At_End()
    {
        var sheet = new Sheet(3, 1);
        sheet.InsertRowAt(3);
        Assert.AreEqual(4, sheet.NumRows);
        sheet.InsertRowAt(10);
        Assert.AreEqual(5, sheet.NumRows);
        sheet.Commands.Undo();
        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumRows);
    }
}