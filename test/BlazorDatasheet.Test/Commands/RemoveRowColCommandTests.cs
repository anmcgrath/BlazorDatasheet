using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class RemoveRowColCommandTests
{
    [Test]
    public void Remove_Col_Then_Undo_Works_Ok()
    {
        var sheet = new Sheet(1, 3);
        sheet.Cells.SetValue(0, 2, "'0,2");
        sheet.Cells.SetValue(0, 3, "'0,3");

        sheet.Columns.RemoveAt(2);

        Assert.AreEqual(2, sheet.NumCols);
        Assert.AreEqual("'0,3", sheet.Cells.GetValue(0, 2));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumCols);
        Assert.AreEqual("'0,2", sheet.Cells.GetValue(0, 2));
        Assert.AreEqual("'0,3", sheet.Cells.GetValue(0, 3));
    }

    [Test]
    public void Remove_Row_Then_Undo_Works_Ok()
    {
        var sheet = new Sheet(3, 1);
        sheet.Cells.SetValue(2, 0, "'2,0");
        sheet.Cells.SetValue(3, 0, "'3,0");
        sheet.Rows.RemoveAt(2);

        Assert.AreEqual(2, sheet.NumRows);
        Assert.AreEqual("'3,0", sheet.Cells.GetValue(2, 0));

        sheet.Commands.Undo();
        Assert.AreEqual(3, sheet.NumRows);
        Assert.AreEqual("'2,0", sheet.Cells.GetValue(2, 0));
        Assert.AreEqual("'3,0", sheet.Cells.GetValue(3, 0));
    }

    [Test]
    public void Remove_Row_Then_Undo_Restores_Formatting()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new Region(1, 1), new CellFormat() { BackgroundColor = "red" });
        sheet.Rows.RemoveAt(1);
        sheet.Commands.Undo();
        Assert.AreEqual("red", sheet.GetFormat(1, 1)?.BackgroundColor);
    }

    [Test]
    public void Remove_Row_With_Row_Format_Removes_And_Restores_Formatting()
    {
        /*
         * Before
         * 0 | 1 | 2 | 3
         0 - | - | - | -
         1 r | r | r | r
         2 r | r | r | r
         3 - | - | - | -
         * After remove row 1
         * 0 | 1 | 2 | 3
         0 - | - | - | -
         1 r | r | r | r
         2 - | - | - | -
         * After undo
         * 0 | 1 | 2 | 3
         0 - | - | - | -
         1 r | r | r | r
         2 r | r | r | r
         3 - | - | - | -
         */
        var sheet = new Sheet(4, 4);
        sheet.SetFormat(new RowRegion(1, 2), new CellFormat() { BackgroundColor = "red" });
        sheet.Rows.RemoveAt(1);
        Assert.AreEqual(new CellFormat(), sheet.GetFormat(0, 0));
        Assert.AreEqual("red", sheet.GetFormat(1, 0)?.BackgroundColor);
        Assert.AreEqual(new CellFormat(), sheet.GetFormat(2, 0));
        sheet.Commands.Undo();
        Assert.AreEqual("red", sheet.GetFormat(1, 0)?.BackgroundColor);
        Assert.AreEqual("red", sheet.GetFormat(2, 0)?.BackgroundColor);
        Assert.AreEqual(new CellFormat(), sheet.GetFormat(3, 0));
    }

    [Test]
    public void Remove_Col_With_Col_Format_Removes_And_Restores_Formatting()
    {
        /*
         * Before
         * 0 | 1 | 2 | 3
         0 - | r | r | -
         1 - | r | r | -
         2 - | r | r | -
         3 - | r | r | -
         * After remove col 1
           * 0 | 1 | 2 | 3
           0 - | r | - | -
           1 - | r | - | -
           2 - | r | - | -
           3 - | r | - | -
         * After undo
           * 0 | 1 | 2 | 3
           0 - | r | r | -
           1 - | r | r | -
           2 - | r | r | -
           3 - | r | r | -
         */
        var sheet = new Sheet(4, 4);
        sheet.SetFormat(new ColumnRegion(1, 2), new CellFormat() { BackgroundColor = "red" });
        sheet.Columns.RemoveAt(1);
        Assert.AreEqual(new CellFormat(), sheet.GetFormat(0, 0));
        Assert.AreEqual("red", sheet.GetFormat(0, 1)?.BackgroundColor);
        Assert.AreEqual(new CellFormat(), sheet.GetFormat(0, 2));
        sheet.Commands.Undo();
        Assert.AreEqual("red", sheet.GetFormat(0, 1)?.BackgroundColor);
        Assert.AreEqual("red", sheet.GetFormat(0, 2)?.BackgroundColor);
        Assert.AreEqual(new CellFormat(), sheet.GetFormat(3, 0));
    }

    [Test]
    public void Remove_Cols_Then_Undo_Restores_Widths()
    {
        var sheet = new Sheet(5, 5);
        sheet.Columns.SetSize(1, 100);
        sheet.Columns.SetSize(2, 200);
        sheet.Columns.RemoveAt(1, 2);
        sheet.Commands.Undo();
        sheet.Columns.GetVisualWidth(1).Should().Be(100);
        sheet.Columns.GetVisualWidth(2).Should().Be(200);
    }

    [Test]
    public void Remove_Rows_Then_Undo_Restores_Heights()
    {
        var sheet = new Sheet(5, 5);
        sheet.Rows.SetSize(1, 100);
        sheet.Rows.SetSize(2, 200);
        sheet.Rows.RemoveAt(1, 2);
        sheet.Commands.Undo();
        sheet.Rows.GetVisualHeight(1).Should().Be(100);
        sheet.Rows.GetVisualHeight(2).Should().Be(200);
    }

    [Test]
    public void Remove_Row_At_End_Of_Format_region_Then_Undo_restores_Format()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(sheet.Range("A1:A5").Region, new CellFormat() { TextAlign = "centre" });
        var cmd = new RemoveRowColsCommand(4, Axis.Row, 1);
        cmd.Execute(sheet);
        cmd.Undo(sheet);
        sheet.GetFormat(4, 0).TextAlign.Should().Be("centre");
    }

    [Test]
    public void Remove_Col_At_End_Of_Format_region_Then_Undo_restores_Format()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(sheet.Range("A1:D1").Region, new CellFormat() { TextAlign = "centre" });
        var cmd = new RemoveRowColsCommand(3, Axis.Col, 1);
        cmd.Execute(sheet);
        cmd.Undo(sheet);
        sheet.GetFormat(0, 3).TextAlign.Should().Be("centre");
    }

    [Test]
    public void Remove_Col_Intersects_With_Edge_Of_region_Restores_On_Undo()
    {
        // <------>
        // | 0 | 1 | 2 | 3 |
        // |   | c | c | c |
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(new Region(0, 0, 1, 3), new CellFormat() { TextAlign = "centre" });
        var cmd = new RemoveRowColsCommand(0, Axis.Col, 2);
        cmd.Execute(sheet);
        cmd.Undo(sheet);
        sheet.GetFormat(0, 1)?.TextAlign.Should().Be("centre");
    }
}