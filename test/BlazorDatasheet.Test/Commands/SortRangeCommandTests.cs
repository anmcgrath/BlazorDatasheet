using System;
using System.Collections.Generic;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Validation;
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
    public void Sort_Command_Moves_Cell_Types_In_Region()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValues(0, 0,
            [["E"], ["D"], ["C"], ["B"], ["A"]]
        );
        sheet.Cells.SetType(new Region(0, 2, 0, 0), "type1");
        sheet.Cells.SetType(new Region(3, 4, 0, 0), "type2");
        sheet.Commands.ExecuteCommand(new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true)));
        sheet.Cells.GetCellType(0, 0).Should().Be("type2");
        sheet.Cells.GetCellType(1, 0).Should().Be("type2");
        sheet.Cells.GetCellType(2, 0).Should().Be("type1");
        sheet.Cells.GetCellType(3, 0).Should().Be("type1");
        sheet.Cells.GetCellType(4, 0).Should().Be("type1");
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

    [Test]
    public void Sort_Command_Moves_Metadata_Without_Leaving_Stale_Data()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;
        sheet.Cells.SetCellMetaData(0, 0, "tag", "first");

        var cmd = new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);

        sheet.Cells[0, 0].Value.Should().Be(1);
        sheet.Cells[1, 0].Value.Should().Be(2);
        sheet.Cells.GetMetaData(0, 0, "tag").Should().BeNull();
        sheet.Cells.GetMetaData(1, 0, "tag").Should().Be("first");

        cmd.Undo(sheet);

        sheet.Cells[0, 0].Value.Should().Be(2);
        sheet.Cells[1, 0].Value.Should().Be(1);
        sheet.Cells.GetMetaData(0, 0, "tag").Should().Be("first");
        sheet.Cells.GetMetaData(1, 0, "tag").Should().BeNull();
    }

    [Test]
    public void Sort_Command_Can_Be_Repeated_Without_Metadata_Sticking_To_Old_Rows()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 3;
        sheet.Cells[1, 0].Value = 1;
        sheet.Cells[2, 0].Value = 2;
        sheet.Cells.SetCellMetaData(0, 0, "tag", "first");

        sheet.Commands.ExecuteCommand(new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true)));

        sheet.Cells.GetMetaData(2, 0, "tag").Should().Be("first");
        sheet.Cells.GetMetaData(0, 0, "tag").Should().BeNull();

        sheet.Commands.ExecuteCommand(new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, false)));

        sheet.Cells[0, 0].Value.Should().Be(3);
        sheet.Cells.GetMetaData(0, 0, "tag").Should().Be("first");
        sheet.Cells.GetMetaData(1, 0, "tag").Should().BeNull();
        sheet.Cells.GetMetaData(2, 0, "tag").Should().BeNull();
    }

    [Test]
    public void Sort_Command_Moves_Validators_And_Restores_On_Undo()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(0, 0, 0, 0), validator);

        var cmd = new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);

        // the validator should have followed the value from row 0 to row 1
        sheet.Validators.Get(1, 0).Should().Contain(validator, "validator follows its cell");
        sheet.Validators.Get(0, 0).Should().BeEmpty("no stale validator left behind");

        cmd.Undo(sheet);

        sheet.Validators.Get(0, 0).Should().Contain(validator, "validator restored on undo");
        sheet.Validators.Get(1, 0).Should().BeEmpty("no stale validator after undo");
    }

    [Test]
    public void Sort_Command_Revalidates_After_Moving_Validators()
    {
        var sheet = new Sheet(4, 2);
        sheet.Cells[0, 0].Value = 1;
        sheet.Cells[2, 0].Value = 2;
        sheet.Cells[2, 1].Value = "not a number";
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(2, 2, 1, 1), validator);

        var cmd = new SortRangeCommand(new Region(0, 3, 0, 1), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);

        sheet.Cells[1, 1].IsValid.Should().BeFalse("the validator followed the invalid value");

        cmd.Undo(sheet);

        sheet.Cells[2, 1].IsValid.Should().BeFalse(
            "undo revalidated the original row outside the compacted sorted region");
    }

    [Test]
    public void Sort_Command_Does_Not_Duplicate_Validators_After_Redo_And_Undo()
    {
        var sheet = new Sheet(3, 1);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(0, 0, 0, 0), validator);
        sheet.Commands.ExecuteCommand(
            new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true)));

        sheet.Commands.Undo().Should().BeTrue();
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Commands.Undo().Should().BeTrue();

        sheet.Validators.Get(0, 0).Should().ContainSingle().Which.Should().BeSameAs(validator);
        sheet.Validators.Get(1, 0).Should().BeEmpty();
    }

    [Test]
    public void Sort_Command_Keeps_Validator_On_Empty_Cell_In_Sorted_Region()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;
        // row 2 col 0 is EMPTY but carries a validator
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(2, 2, 0, 0), validator);

        var cmd = new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);
        cmd.Undo(sheet);

        sheet.Validators.Get(2, 0).Should().Contain(validator, "validator on empty cell survives sort+undo");
    }

    [Test]
    public void Sort_Command_Keeps_Validator_On_Valueless_Column_In_Sorted_Region()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;
        // col 1 has no values at all, but row 0 col 1 carries a validator
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(0, 0, 1, 1), validator);

        var cmd = new SortRangeCommand(new Region(0, 4, 0, 1), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);
        cmd.Undo(sheet);

        sheet.Validators.Get(0, 1).Should().Contain(validator, "validator in valueless column survives sort+undo");
    }

    [Test]
    public void Sort_Command_Moves_State_On_Valueless_Column_With_Its_Row()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;

        // col 1 holds no values at all, only state, on the row that is about to move
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(0, 0, 1, 1), validator);
        sheet.Cells.SetType(new Region(0, 0, 1, 1), "bool");
        sheet.Cells.SetCellMetaData(0, 1, "tag", "first");

        var cmd = new SortRangeCommand(new Region(0, 4, 0, 1), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);

        // row 0 sorted down to row 1, so its col-1 state must travel with it
        sheet.Validators.Get(1, 1).Should().Contain(validator);
        sheet.Cells.GetCellType(1, 1).Should().Be("bool");
        sheet.Cells.GetMetaData(1, 1, "tag").Should().Be("first");

        // and must not be left behind
        sheet.Validators.Get(0, 1).Should().BeEmpty();
        sheet.Cells.GetCellType(0, 1).Should().Be("default");
        sheet.Cells.GetMetaData(0, 1, "tag").Should().BeNull();

        cmd.Undo(sheet);

        sheet.Validators.Get(0, 1).Should().Contain(validator);
        sheet.Cells.GetCellType(0, 1).Should().Be("bool");
        sheet.Cells.GetMetaData(0, 1, "tag").Should().Be("first");
        sheet.Validators.Get(1, 1).Should().BeEmpty();
        sheet.Cells.GetCellType(1, 1).Should().Be("default");
        sheet.Cells.GetMetaData(1, 1, "tag").Should().BeNull();
    }

    [Test]
    public void Sort_Command_Sorts_Valueless_Row_Carrying_State_To_Bottom()
    {
        var sheet = new Sheet(3, 1);
        // row 0 has no value at all, only a validator - Excel treats it as blank, sorting it last
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new Region(0, 0, 0, 0), validator);
        sheet.Cells[1, 0].Value = 2;
        sheet.Cells[2, 0].Value = 1;

        var cmd = new SortRangeCommand(new ColumnRegion(0), new ColumnSortOptions(0, true));
        cmd.Execute(sheet);

        sheet.Cells[0, 0].Value.Should().Be(1);
        sheet.Cells[1, 0].Value.Should().Be(2);
        sheet.Cells[2, 0].Value.Should().BeNull();

        // the blank row travelled to the bottom and took its validator with it
        sheet.Validators.Get(2, 0).Should().Contain(validator);
        sheet.Validators.Get(0, 0).Should().BeEmpty();

        cmd.Undo(sheet);

        sheet.Validators.Get(0, 0).Should().Contain(validator);
        sheet.Validators.Get(2, 0).Should().BeEmpty();
        sheet.Cells[1, 0].Value.Should().Be(2);
        sheet.Cells[2, 0].Value.Should().Be(1);
    }

    [Test, Timeout(15000)]
    public void Sort_With_Validator_Over_Whole_Column_Completes()
    {
        const int rowCount = 100_000;
        var sheet = new Sheet(rowCount, 2);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[1, 0].Value = 1;
        // an unbounded region - spans every row, so row walks must be clipped to the sheet
        var validator = new NumberValidator(true);
        sheet.Validators.Add(new ColumnRegion(1), validator);

        var cmd = new SortRangeCommand(new ColumnRegion(0, 1), new ColumnSortOptions(0, true));

        Action act = () => cmd.Execute(sheet);
        act.Should().NotThrow();

        sheet.Cells[0, 0].Value.Should().Be(1);
        sheet.Cells[1, 0].Value.Should().Be(2);
        sheet.Validators.Get(0, 1).Should().Contain(validator);
        sheet.Validators.Get(rowCount - 1, 1).Should().Contain(validator);
    }
}
