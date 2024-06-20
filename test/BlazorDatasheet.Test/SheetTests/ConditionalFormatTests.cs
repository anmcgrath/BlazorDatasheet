using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

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
        cm = sheet.ConditionalFormats;
        greaterThanEqualToZeroRedBgCf = new ConditionalFormat(
            (posn, s) => s.Cells.GetCell(posn.row, posn.col).GetValue<int?>() >= 0
            , (cell => new CellFormat
            {
                BackgroundColor = redBgColor
            }));
    }

    [Test]
    public void Set_Cf_To_Whole_Sheet_Applies_Correctly()
    {
        sheet.Cells.SetValue(0, 0, -1);
        cm.Apply(sheet.Region, greaterThanEqualToZeroRedBgCf);
        var format = cm.GetFormatResult(0, 0);
        Assert.IsNull(format);
        sheet.Cells.SetValue(0, 0, 1);

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
        cm.Apply(sheet.Region, cf);
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
        sheet.ConditionalFormats.Apply(new RowRegion(1), cf);
        sheet.Rows.InsertAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("2", sheet.ConditionalFormats.GetFormatResult(2, 0)?.BackgroundColor);
        sheet.Rows.RemoveAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("1", sheet.ConditionalFormats.GetFormatResult(1, 0)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Shifts_When_Col_Inserted_And_Removed_Before()
    {
        
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Col.ToString() });
        sheet.ConditionalFormats.Apply(new ColumnRegion(1), cf);
        sheet.Columns.InsertAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 1)?.BackgroundColor);
        Assert.AreEqual("2", sheet.ConditionalFormats.GetFormatResult(0, 2)?.BackgroundColor);
        sheet.Columns.RemoveAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("1", sheet.ConditionalFormats.GetFormatResult(0, 1)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Expands_When_Col_Inserted_Inside_It()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Col.ToString() });

        sheet.ConditionalFormats.Apply(new Region(1, 2, 1, 2), cf);
        sheet.Columns.InsertAt(1);
        Assert.AreEqual("3", sheet.ConditionalFormats.GetFormatResult(1, 3)?.BackgroundColor);
        sheet.Columns.RemoveAt(2);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(1, 3)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Expands_When_Row_Inserted_Inside_It()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Row.ToString() });

        sheet.ConditionalFormats.Apply(new Region(1, 2, 1, 2), cf);
        sheet.Rows.InsertAt(1);
        Assert.AreEqual("3", sheet.ConditionalFormats.GetFormatResult(3, 1)?.BackgroundColor);
        sheet.Rows.RemoveAt(2);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(3, 1)?.BackgroundColor);
    }
}