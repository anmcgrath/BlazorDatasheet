using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

/// <summary>
/// The dirty event bounds its rows by the columns that were actually marked. The renderer rebuilds
/// a visual cell per dirty position, so a row-wide span makes a one-cell edit cost a whole row.
/// </summary>
public class DirtyRegionTests
{
    private record DirtySnapshot(List<Interval> Rows, int ColStart, int ColEnd)
    {
        public bool IsEmpty => ColEnd < ColStart;
    }

    /// <summary>
    /// DirtyRows is a live buffer that the sheet clears as soon as the event returns, so the rows
    /// have to be copied inside the handler.
    /// </summary>
    private static List<DirtySnapshot> Capture(Sheet sheet)
    {
        var captured = new List<DirtySnapshot>();
        sheet.SheetDirty += (_, e) =>
            captured.Add(new DirtySnapshot(e.DirtyRows.GetAllIntervals(), e.DirtyColStart, e.DirtyColEnd));
        return captured;
    }

    [Test]
    public void Setting_One_Cell_Marks_Only_That_Cell_Dirty()
    {
        var sheet = new Sheet(50, 50);
        var captured = Capture(sheet);

        sheet.Cells.SetValue(10, 20, "x");

        captured.Should().NotBeEmpty();
        var args = captured[^1];
        args.IsEmpty.Should().BeFalse();
        args.ColStart.Should().Be(20);
        args.ColEnd.Should().Be(20);
        args.Rows.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Interval(10, 10));
    }

    [Test]
    public void Setting_A_Region_Marks_That_Regions_Columns()
    {
        var sheet = new Sheet(50, 50);
        var captured = Capture(sheet);

        sheet.Cells.SetValues(new Region(5, 8, 2, 4), 1);

        var args = captured[^1];
        args.ColStart.Should().Be(2);
        args.ColEnd.Should().Be(4);
    }

    [Test]
    public void Marks_Across_Several_Columns_Report_The_Span_Covering_Them()
    {
        var sheet = new Sheet(50, 50);
        var captured = Capture(sheet);

        sheet.BatchUpdates();
        sheet.Cells.SetValue(1, 3, "a");
        sheet.Cells.SetValue(2, 9, "b");
        sheet.EndBatchUpdates();

        // a superset of what changed, so nothing can be missed
        var args = captured[^1];
        args.ColStart.Should().Be(3);
        args.ColEnd.Should().Be(9);
    }

    [Test]
    public void A_Row_Wide_Change_Still_Reports_The_Full_Width()
    {
        var sheet = new Sheet(50, 50);
        var captured = Capture(sheet);

        sheet.Rows.SetSize(4, 40);

        var args = captured[^1];
        args.IsEmpty.Should().BeFalse();
        args.ColStart.Should().Be(0);
        args.ColEnd.Should().Be(int.MaxValue);
    }

    [Test]
    public void A_Formulas_Dependents_Are_Marked_Dirty_In_Their_Own_Columns()
    {
        var sheet = new Sheet(50, 50);
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Cells.SetFormula(0, 5, "=A1+1");

        var captured = Capture(sheet);
        sheet.Cells.SetValue(0, 0, 2);

        // the edit is in column 0 and its dependent is in column 5 - both have to be covered
        var args = captured[^1];
        args.ColStart.Should().Be(0);
        args.ColEnd.Should().BeGreaterThanOrEqualTo(5);
    }
}
