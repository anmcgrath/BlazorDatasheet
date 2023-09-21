using System.Linq;
using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formats;
using BlazorDatasheet.Render;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class ConditionalFormatTests
{
    private ConditionalFormatManager cm;

    /// <summary>
    /// Sheet of size 2 x 2
    /// </summary>
    private Sheet sheet;

    private ConditionalFormat greaterThanEqualToZeroRedBgCf;
    private string redBgColor = "#ff0000";

    [SetUp]
    public void Setup()
    {
        sheet = new Sheet(4, 4);
        cm = new ConditionalFormatManager(sheet);
        greaterThanEqualToZeroRedBgCf = new ConditionalFormat(
            (posn, s) => s.GetCell(posn.row, posn.col).GetValue<int?>() >= 0
            , (cell => new CellFormat
            {
                BackgroundColor = redBgColor
            }));
    }

    [Test]
    public void Set_Cf_To_Whole_Sheet_Applies_Correctly()
    {
        sheet.TrySetCellValue(0, 0, -1);
        cm.Apply(greaterThanEqualToZeroRedBgCf);
        var format = cm.GetFormatResult(0, 0);
        Assert.IsNull(format);
        sheet.TrySetCellValue(0, 0, 1);

        format = cm.GetFormatResult(0, 0);
        Assert.AreEqual(format.BackgroundColor, redBgColor);
    }

    [Test]
    public void Cf_Correctly_Passes_All_Cells_To_Func()
    {
        // Create a conditional format that sets the background color to
        // a string which is equal to the number of cells that have the conditional
        // format registered
        var cf = new ConditionalFormat(
            (posn, sheet) => true, (cell, cells) => new CellFormat() { BackgroundColor = cells.Count().ToString() });
        cf.IsShared = true;
        cm.Apply(cf);
        var formatApplied = cm.GetFormatResult(0, 0);
        Assert.NotNull(formatApplied);
        Assert.AreEqual(sheet.Region.Area.ToString(), formatApplied!.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Shifts_When_Row_Inserted_And_Removed_Before()
    {
        // Create a new conditional format that is always run and sets the background colour to the row number
        var cf = new ConditionalFormat((posn, sheet) => true,
                                       cell => new CellFormat() { BackgroundColor = cell.Row.ToString() });
        // Set this format to the second row in the sheet (sheet has 2 rows)
        sheet.ConditionalFormatting.Apply(cf, new RowRegion(1));
        sheet.InsertRowAfter(0);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("2", sheet.ConditionalFormatting.GetFormatResult(2, 0)?.BackgroundColor);
        sheet.RemoveRow(0);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("1", sheet.ConditionalFormatting.GetFormatResult(1, 0)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Shifts_When_Col_Inserted_And_Removed_Before()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
                                       cell => new CellFormat() { BackgroundColor = cell.Col.ToString() });
        sheet.ConditionalFormatting.Apply(cf, new ColumnRegion(1));
        sheet.InsertColAfter(0);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("2", sheet.ConditionalFormatting.GetFormatResult(0, 2)?.BackgroundColor);
        sheet.RemoveCol(0);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("1", sheet.ConditionalFormatting.GetFormatResult(0, 1)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Expands_When_Col_Inserted_Inside_It()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
                                       cell => new CellFormat() { BackgroundColor = cell.Col.ToString() });

        sheet.ConditionalFormatting.Apply(cf, new Region(1, 2, 1, 2));
        sheet.InsertColAfter(1);
        Assert.AreEqual("3", sheet.ConditionalFormatting.GetFormatResult(1, 3)?.BackgroundColor);
        sheet.RemoveCol(2);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(1, 3)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Expands_When_Row_Inserted_Inside_It()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
                                       cell => new CellFormat() { BackgroundColor = cell.Row.ToString() });

        sheet.ConditionalFormatting.Apply(cf, new Region(1, 2, 1, 2));
        sheet.InsertRowAfter(1);
        Assert.AreEqual("3", sheet.ConditionalFormatting.GetFormatResult(3, 1)?.BackgroundColor);
        sheet.RemoveRow(2);
        Assert.Null(sheet.ConditionalFormatting.GetFormatResult(3, 1)?.BackgroundColor);
    }
}