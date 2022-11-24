using System.Linq;
using BlazorDatasheet.Data;
using BlazorDatasheet.Render;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class FormattingTests
{
    private Sheet _sheet;

    [SetUp]
    public void Setup_Sheet()
    {
        _sheet = new Sheet(10, 10);
    }

    [Test]
    public void Set_Format_On_Cells_Then_Undo_Correct()
    {
        var format = new Format() { BackgroundColor = "red" };
        _sheet.SetFormat(format, new BRangeCell(_sheet, 0, 0));
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(0, 0)?.BackgroundColor);
        // Test the cell next to it to ensure it hasn't changed format
        Assert.AreNotEqual(format.BackgroundColor, _sheet.GetFormat(0, 1)?.BackgroundColor);

        // Undo format
        _sheet.Commands.Undo();
        Assert.AreNotEqual(format.BackgroundColor, _sheet.GetFormat(0, 0)?.BackgroundColor);
    }

    [Test]
    public void Apply_Col_Format_Sets_Format_Correctly()
    {
        var format = new Format() { BackgroundColor = "red" };
        _sheet.SetFormat(format, _sheet.Range(new ColumnRegion(0)));
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(0, 0)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(9, 0)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(100, 0)?.BackgroundColor);
        // Ensure that the bg color wasn't set outside of the column region
        Assert.Null(_sheet.GetFormat(1, 01)?.BackgroundColor);
    }

    [Test]
    public void Apply_Row_Format_Sets_Format_Correctly()
    {
        var format = new Format() { BackgroundColor = "red" };
        _sheet.SetFormat(format, _sheet.Range(new RowRegion(1)));
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(1, 0)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(1, 9)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(1, 100)?.BackgroundColor);
        // Ensure that the bg color wasn't set outside of the row region
        Assert.Null(_sheet.GetFormat(0, 0)?.BackgroundColor);
    }

    [Test]
    public void Apply_Overlapping_Col_And_Row_Formats_Correctly()
    {
        var colFormat = new Format() { BackgroundColor = "red" };
        var rowFormat = new Format() { BackgroundColor = "blue" };
        var rowRange = _sheet.Range(new RowRegion(1));
        var colRange = _sheet.Range(new ColumnRegion(1));
        _sheet.SetFormat(colFormat, colRange);
        _sheet.SetFormat(rowFormat, rowRange);
        // The overlapping cell (at 1, 1) should have the row format
        Assert.AreEqual(rowFormat.BackgroundColor, _sheet.GetFormat(1, 1)?.BackgroundColor);
        // Set the column format over it
        _sheet.SetFormat(colFormat, colRange);
        // The overlapping cell (1, 1) should have the col format
        Assert.AreEqual(colFormat.BackgroundColor, _sheet.GetFormat(1, 1)?.BackgroundColor);
    }

    [Test]
    public void Apply_Row_Formats_Over_Each_Other_Behaves_Correctly()
    {
        // Checks a bug that was found when the row formats apply over each other
        var format1 = new Format() { BackgroundColor = "red" };
        var format2 = new Format() { BackgroundColor = "blue" };
        _sheet.SetFormat(format1, _sheet.Range(new RowRegion(0, 2)));
        _sheet.SetFormat(format2, _sheet.Range(new RowRegion(1)));

        // Check every cell in row 0 and 2 have format 1 bg color
        var r0cells = _sheet.GetCellsInRegion(new RowRegion(0));
        var formats = r0cells.Select(x => _sheet.GetFormat(x.Row, x.Col));
        Assert.True(_sheet.GetCellsInRegion(new RowRegion(0)).Select(x => _sheet.GetFormat(x.Row, x.Col))
                          .All(f => f?.BackgroundColor == format1.BackgroundColor));
        Assert.True(_sheet.GetCellsInRegion(new RowRegion(2)).Select(x => _sheet.GetFormat(x.Row, x.Col))
                          .All(f => f?.BackgroundColor == format1.BackgroundColor));

        // Check every cell in row 1 have format 2 bg color
        Assert.True(_sheet.GetCellsInRegion(new RowRegion(1)).Select(x => _sheet.GetFormat(x.Row, x.Col))
                          .All(f => f?.BackgroundColor == format2.BackgroundColor));
    }
}