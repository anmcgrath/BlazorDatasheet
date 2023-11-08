using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MergeCellsCommandTests
{
    [Test]
    public void Merge_Cells_Command_Runs_And_Undos_Correctly()
    {
        var sheet = new Sheet(10, 10);

        sheet.Cells.SetValue(0, 0, "'00");
        sheet.Cells.SetValue(2, 2, "'22");

        sheet.Cells.Merge(new Region(0, 2, 0, 2));

        Assert.True(sheet.Cells.IsInsideMerge(0, 0));
        Assert.True(sheet.Cells.IsInsideMerge(2, 2));
        Assert.False(sheet.Cells.IsInsideMerge(3, 3));

        Assert.AreEqual("'00", sheet.Cells.GetValue(0, 0));
        Assert.AreEqual(null, sheet.Cells.GetValue(2, 2));

        sheet.Commands.Undo();

        Assert.False(sheet.Cells.IsInsideMerge(0, 0));

        Assert.AreEqual("'00", sheet.Cells.GetValue(0, 0));
        Assert.AreEqual("'22", sheet.Cells.GetValue(2, 2));
    }
}