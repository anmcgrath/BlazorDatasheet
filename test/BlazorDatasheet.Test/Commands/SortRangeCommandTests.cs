using System;
using System.Collections.Generic;
using BlazorDatasheet.Core.Commands;
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
            new ColumnSortOptions(0, true)
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
}