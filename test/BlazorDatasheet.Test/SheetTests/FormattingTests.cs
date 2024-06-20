using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

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
        var format = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new Region(0, 0), format);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(0, 0)?.BackgroundColor);
        // Test the cell next to it to ensure it hasn't changed format
        Assert.AreNotEqual(format.BackgroundColor, _sheet.GetFormat(0, 1)?.BackgroundColor);

        // Undo format
        _sheet.Commands.Undo();
        Assert.AreNotEqual(format.BackgroundColor, _sheet.GetFormat(0, 0)?.BackgroundColor);
    }

    [Test]
    public void Set_Col_Format_On_Cells_Then_Undo_Correct()
    {
        var format = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new ColumnRegion(2, 4), format);
        _sheet.Commands.Undo();
        _sheet.GetFormat(2, 2)?.BackgroundColor?.Should().BeNull();
    }

    [Test]
    public void Set_Row_Format_On_Cells_Then_Undo_Correct()
    {
        var format = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new RowRegion(2, 4), format);
        _sheet.Commands.Undo();
        _sheet.GetFormat(2, 2)?.BackgroundColor?.Should().BeNull();
    }

    [Test]
    public void Apply_Col_Format_On_Intersecting_Cell_Format_Sets_Correctly()
    {
        var cellFormat = new CellFormat() { BackgroundColor = "cell-format-bg" };
        var colFormat = new CellFormat() { BackgroundColor = "col-format-bg" };
        var cellRegion = new Region(2, 4, 2, 4);
        var colRegion = new ColumnRegion(2);

        _sheet.SetFormat(cellRegion, cellFormat);
        _sheet.SetFormat(colRegion, colFormat);

        _sheet.GetFormat(2, 3)?.BackgroundColor.Should().Be(cellFormat.BackgroundColor);
    }


    [Test]
    public void Apply_Col_Format_Sets_Format_Correctly()
    {
        var format = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new ColumnRegion(0), format);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(0, 0)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(9, 0)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(100, 0)?.BackgroundColor);
        // Ensure that the bg color wasn't set outside of the column region
        Assert.Null(_sheet.GetFormat(1, 01)?.BackgroundColor);
    }

    [Test]
    public void Apply_Col_Format_Over_Cell_Format_Then_Undo_Sets_Correctly()
    {
        var cellFormat = new CellFormat() { BackgroundColor = "blue" };
        var colFormat = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new Region(2, 4, 2, 2), cellFormat);
        _sheet.SetFormat(new ColumnRegion(2), colFormat);
        _sheet.GetFormat(2, 2)?.BackgroundColor.Should().Be("red");
        _sheet.Commands.Undo();
        _sheet.GetFormat(2, 2)?.BackgroundColor.Should().Be("blue");
    }

    [Test]
    public void Apply_Row_Format_Sets_Format_Correctly()
    {
        var format = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new RowRegion(1), format);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(1, 0)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(1, 9)?.BackgroundColor);
        Assert.AreEqual(format.BackgroundColor, _sheet.GetFormat(1, 100)?.BackgroundColor);
        // Ensure that the bg color wasn't set outside of the row region
        Assert.Null(_sheet.GetFormat(0, 0)?.BackgroundColor);
    }

    [Test]
    public void Apply_Overlapping_Col_And_Row_Formats_Correctly()
    {
        var colFormat = new CellFormat() { BackgroundColor = "red" };
        var rowFormat = new CellFormat() { BackgroundColor = "blue" };
        var rowRange = new RowRegion(1);
        var colRange = new ColumnRegion(1);
        _sheet.SetFormat(colRange, colFormat);
        _sheet.SetFormat(rowRange, rowFormat);
        // The overlapping cell (at 1, 1) should have the row format
        Assert.AreEqual(rowFormat.BackgroundColor, _sheet.GetFormat(1, 1)?.BackgroundColor);
        // Set the column format over it
        _sheet.SetFormat(colRange, colFormat);
        // The overlapping cell (1, 1) should have the col format
        Assert.AreEqual(colFormat.BackgroundColor, _sheet.GetFormat(1, 1)?.BackgroundColor);
    }

    [Test]
    public void Apply_Row_Formats_Over_Each_Other_Behaves_Correctly()
    {
        // Checks a bug that was found when the row formats apply over each other
        var format1 = new CellFormat() { BackgroundColor = "red" };
        var format2 = new CellFormat() { BackgroundColor = "blue" };
        _sheet.SetFormat(new RowRegion(0, 2), format1);
        _sheet.SetFormat(new RowRegion(1), format2);

        // Check every cell in row 0 and 2 have format 1 bg color
        var r0cells = _sheet.Cells.GetCellsInRegion(new RowRegion(0));
        var formats = r0cells.Select(x => _sheet.GetFormat(x.Row, x.Col));
        Assert.True(_sheet.Cells.GetCellsInRegion(new RowRegion(0)).Select(x => _sheet.GetFormat(x.Row, x.Col))
            .All(f => f?.BackgroundColor == format1.BackgroundColor));
        Assert.True(_sheet.Cells.GetCellsInRegion(new RowRegion(2)).Select(x => _sheet.GetFormat(x.Row, x.Col))
            .All(f => f?.BackgroundColor == format1.BackgroundColor));

        // Check every cell in row 1 have format 2 bg color
        Assert.True(_sheet.Cells.GetCellsInRegion(new RowRegion(1)).Select(x => _sheet.GetFormat(x.Row, x.Col))
            .All(f => f?.BackgroundColor == format2.BackgroundColor));
    }

    [Test]
    public void Insert_ROw_Or_Col_Before_Row_Col_Format_Shifts_Formats()
    {
        var f1 = new CellFormat() { BackgroundColor = "red" };
        var f2 = new CellFormat() { BackgroundColor = "blue" };
        _sheet.SetFormat(new ColumnRegion(2), f1);
        _sheet.SetFormat(new RowRegion(2), f2);
        _sheet.Columns.InsertAt(0);
        _sheet.Rows.InsertAt(0);
        _sheet.GetFormat(0, 2)?.BackgroundColor?.Should().BeNullOrEmpty();
        _sheet.GetFormat(0, 3)?.BackgroundColor.Should().Be("red");

        _sheet.GetFormat(2, 0)?.BackgroundColor?.Should().BeNullOrEmpty();
        _sheet.GetFormat(3, 0)?.BackgroundColor.Should().Be("blue");

        _sheet.Commands.Undo();
        _sheet.Commands.Undo();

        _sheet.GetFormat(0, 2)?.BackgroundColor.Should().Be("red");
        _sheet.GetFormat(0, 3)?.BackgroundColor?.Should().BeNullOrEmpty();

        _sheet.GetFormat(2, 0)?.BackgroundColor.Should().Be("blue");
        _sheet.GetFormat(3, 0)?.BackgroundColor?.Should().BeNullOrEmpty();
    }

    [Test]
    public void Insert_Row_Or_Col_Before_Format_Shifts_Format()
    {
        var f1 = new CellFormat() { BackgroundColor = "red" };
        _sheet.SetFormat(new Region(2, 2), f1);
        _sheet.Columns.InsertAt(0);
        _sheet.Rows.InsertAt(0);
        _sheet.GetFormat(2, 2)?.BackgroundColor?.Should().BeNullOrEmpty();
        _sheet.GetFormat(3, 3)?.BackgroundColor.Should().Be("red");

        _sheet.Commands.Undo();
        _sheet.Commands.Undo();

        _sheet.GetFormat(2, 2)?.BackgroundColor.Should().Be("red");
        _sheet.GetFormat(3, 3)?.BackgroundColor?.Should().BeNullOrEmpty();
    }

    [Test]
    public void Set_Left_Border_Sets_Cell_To_Lefts_Right_Border()
    {
        _sheet.SetFormat(new Region(1, 2),
            new CellFormat()
            {
                BorderLeft = new Border() { Width = 1, Color = "black" },
                BorderRight = new Border() { Width = 1, Color = "black" },
                BorderBottom = new Border() { Width = 1, Color = "black" },
                BorderTop = new Border() { Width = 1, Color = "black" }
            });

        _sheet.GetFormat(1, 2)?.BorderLeft?.Width.Should().Be(1);
        _sheet.GetFormat(1, 2)?.BorderRight?.Width.Should().Be(1);
        _sheet.GetFormat(1, 2)?.BorderBottom?.Width.Should().Be(1);
        _sheet.GetFormat(1, 2)?.BorderTop?.Width.Should().Be(1);

        _sheet.GetFormat(1, 1)?.BorderRight?.Width.Should().Be(1);
        _sheet.GetFormat(1, 1)?.BorderLeft?.Width.Should().Be(0);
        _sheet.GetFormat(1, 1)?.BorderBottom?.Width.Should().Be(0);
        _sheet.GetFormat(1, 1)?.BorderTop?.Width.Should().Be(0);
    }

    [Test]
    public void Override_Cell_Border_Overrides_Border()
    {
        _sheet.SetFormat(new Region(1, 1),
            new CellFormat()
            {
                BorderLeft = new Border() { Width = 1, Color = "black" },
                BorderRight = new Border() { Width = 1, Color = "black" },
                BorderBottom = new Border() { Width = 1, Color = "black" },
                BorderTop = new Border() { Width = 1, Color = "black" }
            });

        _sheet.SetFormat(new Region(1, 1),
            new CellFormat()
            {
                BorderLeft = new Border() { Width = 2, Color = "black" },
                BorderRight = new Border() { Width = 2, Color = "black" },
                BorderBottom = new Border() { Width = 2, Color = "black" },
                BorderTop = new Border() { Width = 2, Color = "black" }
            });

        _sheet.GetFormat(1, 1).BorderLeft.Width.Should().Be(2);
        _sheet.GetFormat(1, 1).BorderRight.Width.Should().Be(2);
        _sheet.GetFormat(1, 1).BorderBottom.Width.Should().Be(2);
        _sheet.GetFormat(1, 1).BorderTop.Width.Should().Be(2);
    }

    [Test]
    public void Set_Format_Undo_Bug()
    {
        _sheet.Range("A1:D7")!.Format = new CellFormat() { BackgroundColor = "red" };
        _sheet.Range("C6:E9")!.Format = new CellFormat() { BackgroundColor = "blue" };
        _sheet.Range("C6")!.Format = new CellFormat() { BackgroundColor = "red" };
        _sheet.Commands.Undo();
        _sheet.Commands.Undo();
        _sheet.Commands.Undo();
        _sheet.GetFormat(0, 0)?.BackgroundColor.Should().Be(null);
    }

    [Test]
    public void Delete_Multiple_Cols_Inside_Format_And_Undo_Resets_Format()
    {
        _sheet.SetFormat(new Region(3, 5, 0, 5), new CellFormat() { BackgroundColor = "red" });
        var existingRegions = _sheet.Cells.GetFormatStore().GetAllDataRegions()
            .Select(x => x.Region.Clone())
            .ToList();

        var cmd = new RemoveRowColsCommand(0, Axis.Col, 2);
        cmd.Execute(_sheet);
        cmd.Undo(_sheet);

        var regionsAfterUndo = _sheet.Cells.GetFormatStore().GetAllDataRegions().Select(x => x.Region.Clone())
            .ToList();

        regionsAfterUndo.Should().BeEquivalentTo(existingRegions);
    }

    [Test]
    public void Insert_Col_Into_Format_Expands_Format()
    {
        _sheet.SetFormat(new Region(0, 0, 0, 1), new CellFormat() { BackgroundColor = "red" });
        _sheet.Columns.InsertAt(1);
        _sheet.GetFormat(0, 2)?.BackgroundColor.Should().Be("red");
        _sheet.Commands.Undo();
        _sheet.Commands.Undo();

        // undo all the way (before setformat) should clear the format.
        _sheet.Cells.GetFormatStore().Any().Should().BeFalse();
    }
}