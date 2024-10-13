using System;
using System.Collections.Generic;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Test.Commands;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Filter;

public class FilterTests
{
    [Test]
    public void Filter_Handler_Matches_Simple_Multi_Column()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValues(0, 0,
        [
            ["out", "in"],
            ["in", "out"],
            ["in", "out"],
            ["out", "in"],
        ]);
        var patternFilter = new SimpleFilter(x => x.ToString().Contains("in"), includeBlanks: false);
        var handler = new FilterHandler();

        handler.GetHiddenRows(sheet, [new(0, patternFilter)])
            .Should()
            .BeEquivalentTo<Interval>([new(0, 0), new(3, sheet.NumRows - 1)]);

        handler.GetHiddenRows(sheet, [new(1, patternFilter)])
            .Should()
            .BeEquivalentTo<Interval>([new(1, 2), new(4, sheet.NumRows - 1)]);
    }

    [Test]
    public void Filter_Handler_With_No_Blanks_Handles_NonContinuousRows()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValue(1, 0, "A2");
        sheet.Cells.SetValue(3, 0, "A4");

        var filter = new SimpleFilter(x => true, includeBlanks: false);
        var handler = new FilterHandler();

        handler.GetHiddenRows(sheet, 0, filter)
            .Should()
            .BeEquivalentTo<Interval>([new(0, 0), new(2, 2), new(4, 99)]);
    }

    [Test]
    public void Filter_Handler_Matches_When_Single_Value()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValues(0, 0,
        [
            ["out", "in"],
        ]);

        var patternFilter = new SimpleFilter(x => x.ToString().Contains("in"), includeBlanks: false);
        var handler = new FilterHandler();

        handler.GetHiddenRows(sheet, 0, patternFilter)
            .Should().BeEquivalentTo<Interval>(
                [new(0, 99)]);

        handler.GetHiddenRows(sheet, 1, patternFilter)
            .Should().BeEquivalentTo<Interval>(
                [new(1, 99)]);
    }

    [Test]
    public void Filter_Handler_When_All_Matches_Hides_Nothing()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetValues(0, 0, [["in"], ["in"], ["in"]]);
        var patternFilter = new SimpleFilter(x => x.ToString().Contains("in"), true);

        var handler = new FilterHandler();
        handler.GetHiddenRows(sheet, 0, patternFilter)
            .Should().BeEmpty();
    }

    [Test]
    public void Filter_Handler_With_Include_Blanks_Includes_Blanks()
    {
        var filter = new OnlyBlanksFilter();
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetValue(5, 0, "(0,5)");

        var handler = new FilterHandler();
        handler.GetHiddenRows(sheet, 0, filter)
            .Should()
            .BeEquivalentTo<Interval>([new(5, 5)]);
    }

    [Test]
    public void Filter_Handler_When_No_Matches_Hides_All()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetValues(0, 0, [["out"], ["out"], ["out"]]);
        var patternFilter = new SimpleFilter(x => x.ToString().Contains("in"), true);

        var handler = new FilterHandler();
        handler.GetHiddenRows(sheet, 0, patternFilter)
            .Should().BeEquivalentTo<Interval>([new(0, sheet.NumRows - 1)]);
    }

    [Test]
    public void Pattern_Filter_Matches()
    {
        TestPatternFilterMatch(PatternFilterType.Contains, "Test", CellValue.Text("Test")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.Contains, "Test", CellValue.Text("")).Should().Be(false);
        TestPatternFilterMatch(PatternFilterType.Contains, "Test", CellValue.Text("A")).Should().Be(false);

        TestPatternFilterMatch(PatternFilterType.NotContains, "Test", CellValue.Text("A")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.NotContains, "Test", CellValue.Text("Test")).Should().Be(false);

        TestPatternFilterMatch(PatternFilterType.NotStartsWith, "Test", CellValue.Text("Test")).Should().Be(false);
        TestPatternFilterMatch(PatternFilterType.NotStartsWith, "Test", CellValue.Text("A")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.NotStartsWith, "Test", CellValue.Text("")).Should().Be(true);

        TestPatternFilterMatch(PatternFilterType.StartsWith, "T", CellValue.Text("Test")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.StartsWith, "T", CellValue.Text("Apple")).Should().Be(false);
        TestPatternFilterMatch(PatternFilterType.StartsWith, "", CellValue.Text("Apple")).Should().Be(true);

        TestPatternFilterMatch(PatternFilterType.EndsWith, "Test", CellValue.Text("Test")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.EndsWith, "Test", CellValue.Text("TheTest")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.EndsWith, "Test", CellValue.Text("TheTests")).Should().Be(false);
        TestPatternFilterMatch(PatternFilterType.EndsWith, "", CellValue.Text("TheTests")).Should().Be(true);

        TestPatternFilterMatch(PatternFilterType.NotEndsWith, "Test", CellValue.Text("Test")).Should().Be(false);
        TestPatternFilterMatch(PatternFilterType.NotEndsWith, "Test", CellValue.Text("TheTest")).Should().Be(false);
        TestPatternFilterMatch(PatternFilterType.NotEndsWith, "Test", CellValue.Text("TheTests")).Should().Be(true);
        TestPatternFilterMatch(PatternFilterType.NotEndsWith, "", CellValue.Text("TheTests")).Should().Be(true);
    }

    [Test]
    public void Insert_Col_Moves_Filter_Correctly_And_Undo()
    {
        var sheet = new Sheet(10, 10);
        var filter = new TestFilter("Test");
        sheet.Columns.Filters.Set(5, filter);

        sheet.Columns.InsertAt(0, 2);
        sheet.Columns.Filters.Get(5).Filters.Should().BeEmpty();
        sheet.Columns.Filters.Get(7).Filters.Should().BeEquivalentTo([filter]);

        sheet.Commands.Undo();
        sheet.Columns.Filters.Get(5).Filters.Should().BeEquivalentTo([filter]);
    }

    [Test]
    public void Remove_Col_Moves_Filter_Correctly_And_Undo()
    {
        var sheet = new Sheet(10, 10);
        var filter = new TestFilter("Test");
        var filter2 = new TestFilter("Test2");

        sheet.Columns.Filters.Set(5, filter);
        sheet.Columns.Filters.Set(7, filter2);

        sheet.Columns.RemoveAt(5, 2);
        sheet.Columns.Filters.Get(5).Filters.Should().BeEquivalentTo([filter2]);
        sheet.Columns.Filters.Get(6).Filters.Should().BeEmpty();
        sheet.Columns.Filters.Get(7).Filters.Should().BeEmpty();

        sheet.Commands.Undo();
        sheet.Columns.Filters.Get(5).Filters.Should().BeEquivalentTo([filter]);
        sheet.Columns.Filters.Get(6).Filters.Should().BeEmpty();
        sheet.Columns.Filters.Get(7).Filters.Should().BeEquivalentTo([filter2]);
    }

    [Test]
    public void Value_Filter_Should_Remove_Rows_With_NoMatch()
    {
        var valueFilter = new ValueFilter();
        var exclude = CellValue.Text("Exclude");

        valueFilter.Exclude(exclude);
        valueFilter.IncludeBlanks = true;

        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(4, 0, "Include");
        sheet.Cells.SetValue(5, 0, exclude);
        var handler = new FilterHandler();
        handler.GetHiddenRows(sheet, 0, valueFilter)
            .Should()
            .BeEquivalentTo<Interval>([new(5, 5)]);
    }

    [Test]
    public void Value_Filter_Matches()
    {
        var valueFilter = new ValueFilter();
        var exclude = CellValue.Number(10);
        var include = CellValue.Number(1);

        valueFilter.Exclude(exclude);
        valueFilter.IncludeBlanks = true;
        valueFilter.IncludeAll = false;

        valueFilter.Match(exclude).Should().BeFalse();
        valueFilter.Match(include).Should().BeTrue();

        valueFilter.IncludeAll = true;
        valueFilter.Match(exclude).Should().BeTrue();
    }

    private bool TestPatternFilterMatch(PatternFilterType type, string value, CellValue testValue)
    {
        return (new PatternFilter(type, value)).Match(testValue);
    }

    private class OnlyBlanksFilter : IFilter
    {
        public bool Match(CellValue cellValue)
        {
            return false;
        }

        public bool IncludeBlanks => true;
        public IFilter Clone() => new OnlyBlanksFilter();
    }

    private class SimpleFilter : IFilter
    {
        private readonly Predicate<CellValue> _matches;
        private readonly bool _includeBlanks;

        public SimpleFilter(Predicate<CellValue> matches, bool includeBlanks)
        {
            _matches = matches;
            _includeBlanks = includeBlanks;
        }

        public bool Match(CellValue cellValue)
        {
            return _matches(cellValue);
        }

        public bool IncludeBlanks => _includeBlanks;

        public IFilter Clone()
        {
            return new SimpleFilter(_matches, _includeBlanks);
        }
    }
}