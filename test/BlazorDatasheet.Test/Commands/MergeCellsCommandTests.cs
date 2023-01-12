using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MergeCellsCommandTests
{
    [Test]
    public void Merge_Cells_Command_Runs_And_Undos_Correctly()
    {
        var sheet = new Sheet(10, 10);

        sheet.TrySetCellValue(0, 0, "00");
        sheet.TrySetCellValue(2, 2, "22");

        sheet.MergeCells(sheet.Range(0, 2, 0, 2));

        Assert.True(sheet.IsPositionMerged(0, 0));
        Assert.True(sheet.IsPositionMerged(2, 2));
        Assert.False(sheet.IsPositionMerged(3, 3));

        Assert.AreEqual("00", sheet.GetCellValue(0, 0));
        Assert.AreEqual(null, sheet.GetCellValue(2, 2));

        sheet.Commands.Undo();

        Assert.False(sheet.IsPositionMerged(0, 0));

        Assert.AreEqual("00", sheet.GetCellValue(0, 0));
        Assert.AreEqual("22", sheet.GetCellValue(2, 2));
    }
}