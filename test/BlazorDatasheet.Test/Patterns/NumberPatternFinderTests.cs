using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Patterns;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Patterns;

public class NumberPatternFinderTests
{
    [Test]
    public void Number_Pattern_Finder_Finds_Patterns_In_Data()
    {
        IReadOnlyCell[] data =
        {
            new CellValueOnly(0, 0, 1d, CellValueType.Number),
            new CellValueOnly(1, 0, 3d, CellValueType.Number),
            new CellValueOnly(2, 0, "text1", CellValueType.Text),
            new CellValueOnly(3, 0, "text2", CellValueType.Text),
            new CellValueOnly(4, 0, 7d, CellValueType.Number),
            new CellValueOnly(5, 0, 8d, CellValueType.Number),
        };

        var patternFinder = new NumberPatternFinder();
        var patterns = patternFinder.Find(data).ToList();

        patterns.Should().HaveCount(2);
        var p1 = patterns.First();
        var p2 = patterns.Last();

        p1.Offsets.Should().BeEquivalentTo(new int[] { 0, 1 });
        p2.Offsets.Should().BeEquivalentTo(new int[] { 4, 5 });

        p1.Should().BeOfType(typeof(NumberRegressionPattern));
        p2.Should().BeOfType(typeof(NumberRegressionPattern));

        var l1 = (p1 as NumberRegressionPattern);
        var l2 = (p2 as NumberRegressionPattern);

        l1.LinearFunction.Gradient.Should().Be(2);
        l2.LinearFunction.Gradient.Should().Be(1);
        l1.LinearFunction.YIntercept.Should().Be(-1);
        l2.LinearFunction.YIntercept.Should().Be(6);
    }

    [Test]
    public void Apply_Number_Pattern_ToAllOffsets_Correctly_Applies_Pattern()
    {
        var offsets = new List<int>() { 0, 1, 2 };
        var values = new List<double>() { 3, 5, 7 };
        var p = new NumberRegressionPattern(offsets, values);

        var sheet = new Sheet(100, 100);

        int nRepeats = 3;

        for (int i = 0; i < nRepeats; i++)
        {
            foreach (var offset in p.Offsets)
            {
                var row = (i + 1) * p.Offsets.Count + offset;
                var col = 0;
                var cellPosition = new CellPosition(row, col);
                var cellData = new CellValueOnly(row, col, values[offset], CellValueType.Number);
                var cmd = p.GetCommand(offset, i, cellData, cellPosition);
                cmd.Should().BeOfType(typeof(SetCellValueCommand));
                (cmd as SetCellValueCommand)!.Execute(sheet);
                sheet.Cells.GetValue(row, col).Should().Be(row * 2 + 3);
            }
        }
    }
}