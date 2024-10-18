using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands.Filters;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class FilterCommandsTests
{
    [Test]
    public void Add_Filter_Command_Adds_Filter_And_Applies()
    {
        var sheet = new Sheet(2, 1);
        sheet.Cells.SetValues(0, 0, [
            ["Test"],
            ["Line 2"]
        ]);

        var cmd = new SetColumnFilterCommand(0, new TestFilter("Test"));
        cmd.Execute(sheet);

        sheet.Rows.GetVisible().Should().BeEquivalentTo<Interval>([new(1, 1)]);

        cmd.Undo(sheet);
        sheet.Rows.GetVisible().Should().BeNullOrEmpty();
    }

    [Test]
    public void Clear_Filter_Command_Clears_Filters_From_Column()
    {
        var sheet = new Sheet(2, 1);

        sheet.Cells.SetValues(0, 0, [
            ["Test"],
            ["Line 2"]
        ]);

        sheet.Columns.Filters.Set(0, new TestFilter("Test"));
        var cmd = new ClearFiltersCommand(0);
        cmd.Execute(sheet);

        sheet.Columns.Filters.Get(0).Filters.Should().BeEmpty();

        // we should have un-applied any filters in the column
        sheet.Rows.IsVisible(0).Should().Be(true);
        sheet.Rows.IsVisible(1).Should().Be(true);

        cmd.Undo(sheet);

        var filter = sheet.Columns.Filters.Get(0);
        filter.Should().NotBeNull();
        filter.Filters.First().Should().BeOfType<TestFilter>();
        filter.Filters.Cast<TestFilter>().First().MatchValue.Should().Be("Test");

        sheet.Rows.GetVisible().Should().BeEquivalentTo<Interval>([new(1, 1)]);
    }

    [Test]
    public void Set_ColumnFilter_Over_Existing_Restores_On_Undo()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValues(0, 0, [
            ["A1", "B2"],
            ["A2"]
        ]);

        var existingFilter = new TestFilter("Test");
        sheet.Columns.Filters.SetImpl(0, [existingFilter]);

        // We have one filter applied, set over it using SetFilterCommand
        var cmd = new SetColumnFilterCommand(0, [new TestFilter("Test Override")]);
        cmd.Execute(sheet);

        cmd.Undo(sheet);
        sheet.Columns.Filters.Get(0).Filters.Should().BeEquivalentTo([existingFilter]);
    }

    [Test]
    public void Clear_All_Filters_And_Undo_Clears_Filters()
    {
        var sheet = new Sheet(10, 10);
        var filter1 = new TestFilter("c1");
        var filter2 = new TestFilter("c2");
        sheet.Columns.Filters.SetImpl(0, [filter1]);
        sheet.Columns.Filters.SetImpl(1, [filter2]);

        var cmd = new ClearFiltersCommand();
        cmd.Execute(sheet);

        sheet.Columns.Filters.GetAll().Should().BeEmpty();

        cmd.Undo(sheet);
        sheet.Columns.Filters.GetAll().SelectMany(x => x.Filters).Should().BeEquivalentTo([filter1, filter2]);
    }
}

public class TestFilter : IFilter
{
    public readonly string MatchValue;

    public TestFilter(string matchValue)
    {
        MatchValue = matchValue;
    }

    public bool Match(CellValue cellValue)
    {
        return cellValue.GetValue<string>() == MatchValue;
    }

    public bool IncludeBlanks => true;
    public IFilter Clone() => new TestFilter(MatchValue);
}