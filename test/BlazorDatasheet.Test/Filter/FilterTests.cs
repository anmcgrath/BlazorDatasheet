using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Intervals;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Filter;

public class FilterTests
{
    [Test]
    public void Pattern_Filter_Matches_Simple_Multi_Column()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValues(0, 0,
        [
            ["out", "in"],
            ["in", "out"],
            ["in", "out"],
            ["out", "in"],
        ]);
        var patternFilter = new PatternFilter(PatternFilterType.Contains, "in");
        sheet.Columns.SetFilter(0, patternFilter);
        sheet.Columns.SetFilter(1, patternFilter);

        sheet.Columns.ApplyFilter(0);
        sheet.Rows.GetVisibleRows()
            .Should()
            .BeEquivalentTo<Interval>([new(1, 2)]);

        sheet.Columns.ApplyFilter(1);
        sheet.Rows.GetVisibleRows()
            .Should()
            .BeEquivalentTo<Interval>([new(0, 0), new(3, 3)]);
    }

    /*[Test]
    public void Pattern_Filter_Matches_When_Single_Value()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValues(0, 0,
        [
            ["out", "in"],
        ]);
        var patternFilter = new PatternFilter(PatternFilterType.Contains, "in");
        patternFilter.GetHiddenRows(sheet, 0)
            .Should().BeEquivalentTo<Interval>(
                [new(0, 99)]);

        patternFilter.GetHiddenRows(sheet, 1)
            .Should().BeEquivalentTo<Interval>(
                [new(1, 99)]);
    }

    [Test]
    public void Pattern_Filter_All_Matches_Hides_Nothing()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetValues(0, 0, [["in"], ["in"], ["in"]]);
        var patternFilter = new PatternFilter(PatternFilterType.Contains, "in");

        patternFilter.GetHiddenRows(sheet, 0)
            .Should().BeEmpty();
    }

    [Test]
    public void Pattern_Filter_All_Matches_Hides_All()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetValues(0, 0, [["out"], ["out"], ["out"]]);
        var patternFilter = new PatternFilter(PatternFilterType.Contains, "in");

        patternFilter.GetHiddenRows(sheet, 0)
            .Should().BeEquivalentTo<Interval>([new(0, sheet.NumRows - 1)]);
    }*/
}