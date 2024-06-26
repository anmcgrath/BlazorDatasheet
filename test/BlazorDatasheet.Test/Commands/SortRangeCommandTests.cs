using System;
using System.Collections.Generic;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SortRangeCommandTests
{
    [Test]
    public void Sort_Range_Values_Only_Sorts()
    {
        var sheet = new Sheet(10, 10);
        for (int row = 0; row < sheet.NumRows; row++)
        {
            // set first row to descending e.g 9, 8, 7, ...
            sheet.Cells[row, 0].Value = sheet.NumRows - row - 1;
            // set second row to ascending e.g 0,1,2...
            sheet.Cells[row, 1].Value = row;
        }

        var region = new ColumnRegion(0, 1);
        var options = new List<ColumnSortOptions>
        {
            new ColumnSortOptions(0, true)
        };

        var sortRangeCommand = new SortRangeCommand(region, options);
        sortRangeCommand.Execute(sheet);

        for (int row = 0; row < sheet.NumRows; row++)
        {
            sheet.Cells[row, 0].Value.Should().Be(row);
            sheet.Cells[row, 1].Value.Should().Be(sheet.NumRows - row - 1);
        }

        sortRangeCommand.Undo(sheet);
        for (int row = 0; row < sheet.NumRows; row++)
        {
            sheet.Cells[row, 0].Value.Should().Be(sheet.NumRows - row - 1);
            sheet.Cells[row, 1].Value.Should().Be(row);
        }
    }

    // test that a sort on a single column with some empty cells works
    [Test]
    public void Sort_Col_With_Empty_Rows_Results_In_Continuous_Rows()
    {
        var sheet = new Sheet(5, 1);
        sheet.Cells[0, 0].Value = 5;
        sheet.Cells[1, 0].Value = 4;
        sheet.Cells[3, 0].Value = 3;

        var region = new ColumnRegion(0, 0);
        var options = new List<ColumnSortOptions>
        {
            new (0, true)
        };

        var cmd = new SortRangeCommand(region, options);
        cmd.Execute(sheet);

        sheet.Cells[0, 0].Value.Should().Be(3);
        sheet.Cells[1, 0].Value.Should().Be(4);
        sheet.Cells[2, 0].Value.Should().Be(5);
        sheet.Cells[3, 0].Value.Should().BeNull();

        cmd.Undo(sheet);
        sheet.Cells[0, 0].Value.Should().Be(5);
        sheet.Cells[1, 0].Value.Should().Be(4);
        sheet.Cells[2, 0].Value.Should().BeNull();
        sheet.Cells[3, 0].Value.Should().Be(3);
    }

    [Test]
    public void Sort_Formula_Adjusts_References()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "=B4+3");
        sheet.Cells.SetFormula(1, 0, "=B6+2");
        sheet.Cells.SetFormula(2, 0, "=B7+1");

        var cmd = new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);

        sheet.Cells.GetFormulaString(0, 0).Should().Be("=B5+1");
        sheet.Cells.GetFormulaString(1, 0).Should().Be("=B6+2");
        sheet.Cells.GetFormulaString(2, 0).Should().Be("=B6+3");

        cmd.Undo(sheet);

        sheet.Cells.GetFormulaString(0, 0).Should().Be("=B4+3");
        sheet.Cells.GetFormulaString(1, 0).Should().Be("=B6+2");
        sheet.Cells.GetFormulaString(2, 0).Should().Be("=B7+1");
    }

    [Test]
    public void Sort_Empty_Sheet_Does_Not_Throw_Exception()
    {
        var sheet = new Sheet(10, 10);

        var region = new ColumnRegion(0, 0);
        var options = new List<ColumnSortOptions>
        {
            new ColumnSortOptions(0, true)
        };

        var sortRangeCommand = new SortRangeCommand(region, options);

        Action act = () => sortRangeCommand.Execute(sheet);
        act.Should().NotThrow();
    }

    [Test]
    public void Sort_Descending_With_Empty_Cells_Puts_Empty_At_End()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[2, 0].Value = 2;
        sheet.Cells[4, 0].Value = 4;

        var so = new ColumnSortOptions(0, false);
        var cmd = new SortRangeCommand(new ColumnRegion(0), so);
        cmd.Execute(sheet);
        sheet.Cells[0, 0].Value.Should().Be(4);
        sheet.Cells[1, 0].Value.Should().Be(2);
        sheet.Cells[2, 0].Value.Should().Be(1);
        sheet.Cells[3, 0].Value.Should().BeNull();
        sheet.Cells[4, 0].Value.Should().BeNull();
    }

    [Test]
    public void Sort_Command_Moves_Cell_Types()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[1, 0].Value = 2;
        sheet.Cells[2, 0].Value = 1;
        sheet.Cells[2, 0].Type = "bool";

        var cmd = new SortRangeCommand(new ColumnRegion(0));
        cmd.Execute(sheet);

        sheet.Cells[0, 0].Type.Should().Be("bool");
        cmd.Undo(sheet);

        sheet.Cells[2, 0].Type.Should().Be("bool");
        sheet.Cells[0, 0].Type.Should().Be("default");
        sheet.Cells[1, 0].Type.Should().Be("default");
    }

    [Test]
    public void Sort_On_Multiple_Columns_Sorts_Correctly()
    {
        var sheet = new Sheet(10, 10);
        sheet.Range("A1:A4")!.Value = 5;
        sheet.Range("B1")!.Value = 2;
        sheet.Range("B4")!.Value = 1;

        var options = new List<ColumnSortOptions>()
        {
            new ColumnSortOptions(0, true),
            new ColumnSortOptions(1, true)
        };
        
        var cmd = new SortRangeCommand(new ColumnRegion(0, 1), options);
        cmd.Execute(sheet);
        
        sheet.Cells[0, 1].Value.Should().Be(1);
        sheet.Cells[1, 1].Value.Should().Be(2);
    }
    
}