using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class RemoveRowCommandTests
{
    [Test]
    public void Remove_Row_Then_Undo_Works_Ok()
    {
        var sheet = new Sheet(3, 1);
        sheet.TrySetCellValue(2, 0, "2,0");
        sheet.TrySetCellValue(3, 0, "3,0");
        sheet.RemoveRow(2);

        Assert.AreEqual(2, sheet.NumRows);
        Assert.AreEqual("3,0", sheet.GetCellValue(2, 0));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumRows);
        Assert.AreEqual("2,0", sheet.GetCellValue(2, 0));
        Assert.AreEqual("3,0", sheet.GetCellValue(3, 0));
    }
}