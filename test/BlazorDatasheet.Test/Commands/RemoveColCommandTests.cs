using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formats;
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
        Assert.AreEqual("0,3", sheet.GetValue(0, 2));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumCols);
        Assert.AreEqual("0,2", sheet.GetValue(0, 2));
        Assert.AreEqual("0,3", sheet.GetValue(0, 3));
    }

    [Test]
    public void Remove_Row_Then_Undo_Restores_Formatting()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new CellFormat() { BackgroundColor = "red" }, sheet.Range(1, 1));
        sheet.RemoveRow(1);
        sheet.Commands.Undo();
        Assert.AreEqual("red", sheet.GetFormat(1, 1)?.BackgroundColor);
    }

    [Test]
    public void Remove_Row_With_Row_Format_Removes_And_Restores_Formatting()
    {
        /*
         * Before
         * 0 | 1 | 2 | 3
         * - | - | - | -
         * r | r | r | r
         * r | r | r | r
         * - | - | - | -
         * After remove row 1
         * 0 | 1 | 2 | 3
         * - | - | - | -
         * r | r | r | r
         * - | - | - | -
         * After undo
         * 0 | 1 | 2 | 3
         * - | - | - | -
         * r | r | r | r
         * r | r | r | r
         * - | - | - | -
         */
        var sheet = new Sheet(4, 4);
        sheet.SetFormat(new CellFormat() { BackgroundColor = "red" }, sheet.Range(new RowRegion(1, 2)));
        sheet.RemoveRow(1);
        Assert.Null(sheet.GetFormat(0, 0));
        Assert.AreEqual("red", sheet.GetFormat(1, 0)?.BackgroundColor);
        Assert.Null(sheet.GetFormat(2, 0));
        sheet.Commands.Undo();
        Assert.AreEqual("red", sheet.GetFormat(1, 0)?.BackgroundColor);
        Assert.AreEqual("red", sheet.GetFormat(2, 0)?.BackgroundColor);
        Assert.Null(sheet.GetFormat(3, 0));
    }
}