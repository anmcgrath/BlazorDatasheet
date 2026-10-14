using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SpillStoreTests
{
    private Sheet _sheet;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(10, 10);
    }

    [Test]
    public void Set_Spill_Is_Found_From_Any_Cell_Inside_It()
    {
        var anchor = new CellPosition(1, 1);
        var region = new Region(1, 3, 1, 1);
        _sheet.Cells.SetSpillImpl(anchor, region, blocked: false);

        _sheet.Cells.GetSpillRegion(2, 1).Should().BeEquivalentTo(region);
        _sheet.Cells.GetSpillAnchor(3, 1).Should().Be(anchor);
        _sheet.Cells.GetSpillRegionForAnchor(1, 1).Should().BeEquivalentTo(region);
        _sheet.Cells.GetSpillRegionForAnchor(2, 1).Should().BeNull();
        _sheet.Cells.IsSpillAnchor(1, 1).Should().BeTrue();
        _sheet.Cells.IsSpilledCell(1, 1).Should().BeFalse();
        _sheet.Cells.IsSpilledCell(2, 1).Should().BeTrue();
        _sheet.Cells.IsSpilledCell(4, 1).Should().BeFalse();
        _sheet.Cells.AnySpills(new Region(3, 5, 0, 5)).Should().BeTrue();
        _sheet.Cells.AnySpills(new Region(4, 5, 0, 5)).Should().BeFalse();
    }

    [Test]
    public void Blocked_Spill_Owns_No_Cells_But_Is_Found_By_Intersection()
    {
        var anchor = new CellPosition(1, 1);
        var region = new Region(1, 3, 1, 1);
        _sheet.Cells.SetSpillImpl(anchor, region, blocked: true);

        _sheet.Cells.GetSpillRegion(2, 1).Should().BeNull();
        _sheet.Cells.IsSpilledCell(2, 1).Should().BeFalse();
        _sheet.Cells.IsSpillAnchor(1, 1).Should().BeFalse();
        _sheet.Cells.AnySpills(region).Should().BeFalse();
        _sheet.Cells.GetSpillAnchorsIntersecting(new Region(3, 3, 1, 1)).Should().Equal(anchor);
    }

    [Test]
    public void Clearing_A_Spill_Returns_Its_Region_And_Fires_Event()
    {
        var anchor = new CellPosition(0, 0);
        var region = new Region(0, 1, 0, 1);
        _sheet.Cells.SetSpillImpl(anchor, region, blocked: false);

        IRegion? eventOld = null;
        _sheet.Cells.SpillChanged += (_, e) => eventOld = e.OldRegion;
        _sheet.Cells.ClearSpillImpl(anchor).Should().BeEquivalentTo(region);
        eventOld.Should().BeEquivalentTo(region);
        _sheet.Cells.GetSpillRegion(1, 1).Should().BeNull();
    }

    [Test]
    public void Wiping_A_Spill_Empties_Its_Cells_Except_The_Anchor()
    {
        var anchor = new CellPosition(0, 0);
        _sheet.Cells.SetSpillImpl(anchor, new Region(0, 2, 0, 0), blocked: false);
        _sheet.Cells.SetCalculatedValueImpl(0, 0, CellValue.Number(1));
        _sheet.Cells.SetCalculatedValueImpl(1, 0, CellValue.Number(2));
        _sheet.Cells.SetCalculatedValueImpl(2, 0, CellValue.Number(3));

        _sheet.Cells.WipeSpillForAnchor(anchor);

        _sheet.Cells.GetValue(0, 0).Should().Be(1);
        _sheet.Cells.GetValue(1, 0).Should().BeNull();
        _sheet.Cells.GetValue(2, 0).Should().BeNull();
        _sheet.Cells.GetSpillEntry(anchor).Should().BeNull();
    }

    [Test]
    public void Wiping_Intersecting_Spills_Marks_Them_Blocked_And_Keeps_The_Region()
    {
        var anchor = new CellPosition(0, 0);
        var region = new Region(0, 2, 0, 0);
        _sheet.Cells.SetSpillImpl(anchor, region, blocked: false);
        _sheet.Cells.SetCalculatedValueImpl(2, 0, CellValue.Number(3));

        _sheet.Cells.WipeSpillsIntersecting(new Region(2, 2, 0, 0));

        _sheet.Cells.GetValue(2, 0).Should().BeNull();
        _sheet.Cells.IsSpilledCell(2, 0).Should().BeFalse();
        var entry = _sheet.Cells.GetSpillEntry(anchor);
        entry.Should().NotBeNull();
        entry!.Data.IsBlocked.Should().BeTrue();
        entry.Region.Should().BeEquivalentTo(region);
    }

    [Test]
    public void Inserting_A_Row_Above_Shifts_The_Spill_And_Its_Anchor()
    {
        _sheet.Cells.SetSpillImpl(new CellPosition(2, 2), new Region(2, 4, 2, 3), blocked: false);
        _sheet.Rows.InsertAt(0, 2);

        _sheet.Cells.GetSpillRegionForAnchor(2, 2).Should().BeNull();
        _sheet.Cells.GetSpillRegionForAnchor(4, 2).Should().BeEquivalentTo(new Region(4, 6, 2, 3));
        _sheet.Cells.GetSpillAnchor(6, 3).Should().Be(new CellPosition(4, 2));

        _sheet.Commands.Undo();
        _sheet.Cells.GetSpillRegionForAnchor(2, 2).Should().BeEquivalentTo(new Region(2, 4, 2, 3));
    }

    [Test]
    public void Removing_The_Anchor_Row_Removes_The_Spill_And_Its_Values()
    {
        var anchor = new CellPosition(2, 2);
        _sheet.Cells.SetSpillImpl(anchor, new Region(2, 4, 2, 2), blocked: false);
        _sheet.Cells.SetCalculatedValueImpl(3, 2, CellValue.Number(2));
        _sheet.Cells.SetCalculatedValueImpl(4, 2, CellValue.Number(3));

        _sheet.Rows.RemoveAt(2);

        _sheet.Cells.GetSpillStore().Any().Should().BeFalse();
        _sheet.Cells.GetValue(2, 2).Should().BeNull();
        _sheet.Cells.GetValue(3, 2).Should().BeNull();
    }

    [Test]
    public void Removing_A_Row_Below_The_Anchor_Contracts_The_Spill()
    {
        _sheet.Cells.SetSpillImpl(new CellPosition(2, 2), new Region(2, 4, 2, 2), blocked: false);
        _sheet.Rows.RemoveAt(3);
        _sheet.Cells.GetSpillRegionForAnchor(2, 2).Should().BeEquivalentTo(new Region(2, 3, 2, 2));
    }
    [TestCase(false)]
    [TestCase(true)]
    public void Repeating_An_Unchanged_Spill_Does_Not_Emit_An_Event(bool blocked)
    {
        var anchor = new CellPosition(0, 0);
        var region = new Region(0, 2, 0, 0);
        _sheet.Cells.SetSpillImpl(anchor, region, blocked);
        var changes = 0;
        _sheet.Cells.SpillChanged += (_, _) => changes++;

        _sheet.Cells.SetSpillImpl(anchor, region.Clone(), blocked);
        changes.Should().Be(0);
        _sheet.Cells.SetSpillImpl(anchor, region, !blocked);
        changes.Should().Be(1);
        _sheet.Cells.SetSpillImpl(anchor, new Region(0, 3, 0, 0), !blocked);
        changes.Should().Be(2);
    }
}
