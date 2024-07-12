using System.Collections.Generic;
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
        var handler = new FilterHandler();

        handler.GetHiddenRows(sheet, new() { { 0, patternFilter } })
            .Should()
            .BeEquivalentTo<Interval>([new(0, 0), new(3, sheet.NumRows - 1)]);

        handler.GetHiddenRows(sheet, new() { { 1, patternFilter } })
            .Should()
            .BeEquivalentTo<Interval>([new(1, 2), new(4, sheet.NumRows - 1)]);
    }

    [Test]
    public void Pattern_Filter_Matches_When_Single_Value()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValues(0, 0,
        [
            ["out", "in"],
        ]);

        var patternFilter = new PatternFilter(PatternFilterType.Contains, "in");
        var handler = new FilterHandler();
        handler.GetHiddenRows(sheet, new() { { 0, patternFilter } })
            .Should().BeEquivalentTo<Interval>(
                [new(0, 99)]);

        handler.GetHiddenRows(sheet, new() { { 1, patternFilter } })
            .Should().BeEquivalentTo<Interval>(
                [new(1, 99)]);
    }

    /*[Test]
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