using BlazorDatasheet.Core.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MergeCellsCommandTests
{
    [Test]
    public void Merge_Cells_Command_Runs_And_Undos_Correctly()
    {
        var sheet = new Sheet(10, 10);

        sheet.SetCellValue(0, 0, "00");
        sheet.SetCellValue(2, 2, "22");

        sheet.Merges.Add(sheet.Range(0, 2, 0, 2));

        Assert.True(sheet.Merges.IsInsideMerge(0, 0));
        Assert.True(sheet.Merges.IsInsideMerge(2, 2));
        Assert.False(sheet.Merges.IsInsideMerge(3, 3));

        Assert.AreEqual("00", sheet.GetValue(0, 0));
        Assert.AreEqual(null, sheet.GetValue(2, 2));

        sheet.Commands.Undo();

        Assert.False(sheet.Merges.IsInsideMerge(0, 0));

        Assert.AreEqual("00", sheet.GetValue(0, 0));
        Assert.AreEqual("22", sheet.GetValue(2, 2));
    }
}