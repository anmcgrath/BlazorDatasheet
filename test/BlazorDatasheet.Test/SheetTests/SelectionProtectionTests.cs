using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SelectionProtectionTests
{
    private Sheet _sheet = null!;
    private SelectionInputManager _manager = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(10, 10);
        _manager = new SelectionInputManager(_sheet.Selection);
    }

    private void Unlock(IRegion region) => _sheet.SetFormat(region, new CellFormat { IsLocked = false });

    private void Lock(IRegion region) => _sheet.SetFormat(region, new CellFormat { IsLocked = true });

    private void ProtectRestricted() =>
        _sheet.Protection.Protect(new SheetProtectionOptions { AllowSelectLockedCells = false });

    private void Click(int row, int col, bool ctrl = false, bool shift = false)
    {
        _manager.HandlePointerDown(row, col, shift, ctrl, false, 0);
        _manager.HandleWindowMouseUp();
    }

    [Test]
    public void Locked_Cells_Are_Selectable_By_Default()
    {
        new SheetProtectionOptions().AllowSelectLockedCells.Should().BeTrue();
        _sheet.Protection.Protect();
        _sheet.Protection.Can(SheetOperation.SelectLockedCells).Should().BeTrue();
        _sheet.Protection.CanSelect(new Region(3, 3)).Should().BeTrue();

        Click(3, 3);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(3, 3));

        ProtectRestricted();
        _sheet.Protection.Can(SheetOperation.SelectLockedCells).Should().BeFalse();
        _sheet.Protection.CanSelect(new Region(3, 3)).Should().BeFalse();
    }

    [Test]
    public void Click_On_Locked_Cell_Is_Rejected_Without_Notifications()
    {
        Unlock(new Region(0, 0));
        ProtectRestricted();
        _sheet.Selection.Set(0, 0);

        var notifications = 0;
        _sheet.Selection.SelectionChanged += (_, _) => notifications++;
        _sheet.Selection.SelectingChanged += (_, _) => notifications++;
        _sheet.Selection.ActiveCellPositionChanged += (_, _) => notifications++;
        _sheet.Selection.ActiveRegionChanged += (_, _) => notifications++;

        Click(3, 3);
        Click(3, 3, ctrl: true);

        notifications.Should().Be(0);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(0, 0));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 0));
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [Test]
    public void Drag_Stops_At_Locked_Cells_And_Resumes_From_Last_Accepted_Preview()
    {
        Unlock(new Region(0, 3, 0, 3));
        ProtectRestricted();

        _manager.HandlePointerDown(1, 1, false, false, false, 0);
        _manager.HandlePointerOver(2, 2);
        _sheet.Selection.SelectingRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));

        _manager.HandlePointerOver(2, 5);
        _sheet.Selection.SelectingRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));

        _manager.HandlePointerOver(3, 3);
        _manager.HandleWindowMouseUp();

        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 3, 1, 3));
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [Test]
    public void Shift_Extension_Into_Locked_Cells_Is_Rejected()
    {
        Unlock(new Region(0, 3, 0, 3));
        ProtectRestricted();
        _sheet.Selection.Set(1, 1);

        _manager.HandleArrowKeyDown(true, new Offset(0, 1));
        _manager.HandleArrowKeyDown(true, new Offset(0, 1));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1, 1, 3));

        _manager.HandleArrowKeyDown(true, new Offset(0, 1));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1, 1, 3));

        Click(5, 5, shift: true);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1, 1, 3));
    }

    [Test]
    public void Arrow_Navigation_Skips_Over_Locked_Cells()
    {
        Unlock(new Region(0, 0));
        Unlock(new Region(0, 3));
        ProtectRestricted();
        _sheet.Selection.Set(0, 0);

        _manager.HandleArrowKeyDown(false, new Offset(0, 1));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 3));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(0, 3));

        // nothing unlocked remains to the right, so the selection stays put
        _manager.HandleArrowKeyDown(false, new Offset(0, 1));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 3));

        _manager.HandleArrowKeyDown(false, new Offset(0, -1));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 0));
    }

    [Test]
    public void Tab_Navigation_Skips_Locked_Cells_And_Cycles_Regions()
    {
        Unlock(new Region(0, 0));
        Unlock(new Region(2, 2));
        ProtectRestricted();
        _sheet.Selection.Set(new List<IRegion> { new Region(0, 0, 0, 1), new Region(2, 2) });
        _sheet.Selection.Activate(0, 0);

        _manager.HandleTabEnterNavigation(Axis.Col, 1);
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(2, 2));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(2, 2));
    }

    [Test]
    public void Navigation_Over_Entirely_Locked_Regions_Is_A_No_Op()
    {
        Unlock(new Region(9, 9));
        ProtectRestricted();
        _sheet.Selection.Set(new List<IRegion> { new Region(5, 5), new Region(6, 6) });
        _sheet.Selection.Activate(5, 5);

        var notifications = 0;
        _sheet.Selection.SelectionChanged += (_, _) => notifications++;
        _sheet.Selection.ActiveCellPositionChanged += (_, _) => notifications++;
        _sheet.Selection.ActiveRegionChanged += (_, _) => notifications++;

        _manager.HandleTabEnterNavigation(Axis.Col, 1);
        _manager.HandleTabEnterNavigation(Axis.Row, 1);

        notifications.Should().Be(0);
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(5, 5));
        _sheet.Selection.Regions.Should().HaveCount(2);
    }

    [Test]
    public void Header_Selection_Requires_The_Whole_Row_Or_Column_To_Be_Unlocked()
    {
        Unlock(new ColumnRegion(2));
        Unlock(new Region(4, 4, 0, 9));
        ProtectRestricted();
        _sheet.Selection.Set(new ColumnRegion(2));

        Click(-1, 3);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(2, 2));

        Click(3, -1);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(2, 2));

        Click(4, -1);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new RowRegion(4, 4));

        Click(-1, 2);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new ColumnRegion(2, 2));
    }

    [Test]
    public void Consumer_Replacement_Containing_Locked_Cells_Is_Rejected()
    {
        Unlock(new Region(0, 0));
        ProtectRestricted();
        _sheet.Selection.Set(0, 0);
        _sheet.BeforeSelectionInput += (_, e) =>
        {
            e.ProposedRegions = new IRegion[] { new Region(7, 8, 7, 8) };
            e.ProposedActiveRegionIndex = 0;
            e.ProposedActiveCellPosition = new CellPosition(7, 7);
        };

        Click(0, 0);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(0, 0));
    }

    [Test]
    public void Protecting_Moves_The_Selection_To_The_First_Unlocked_Cell()
    {
        Unlock(new Region(4, 4));
        _sheet.Selection.Set(0, 0);
        ProtectRestricted();

        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(4, 4));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(4, 4));

        _sheet.Protection.Unprotect();
        Click(1, 1);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 1));
    }

    [Test]
    public void Protecting_Clears_The_Selection_When_Nothing_Is_Unlocked()
    {
        _sheet.Selection.Set(0, 0);
        _sheet.Selection.BeginSelectingCell(1, 1);
        ProtectRestricted();

        _sheet.Selection.IsEmpty().Should().BeTrue();
        _sheet.Selection.IsSelecting.Should().BeFalse();
    }

    [Test]
    public void First_Unlocked_Cell_Is_Row_Major_Across_Cell_Column_And_Row_Formats()
    {
        _sheet.SetFormat(new RowRegion(1), new CellFormat { IsLocked = false });
        Lock(new Region(1, 1, 0, 4));
        Unlock(new Region(3, 3, 2, 2));
        _sheet.Selection.Set(0, 0);
        ProtectRestricted();

        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 5));
    }

    [Test]
    public void Locks_Changed_In_A_Trusted_Update_Relocate_The_Selection()
    {
        Unlock(new Region(0, 0));
        Unlock(new Region(5, 5));
        _sheet.Selection.Set(0, 0);
        ProtectRestricted();
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 0));

        _sheet.Protection.RunUnprotected(() => Lock(new Region(0, 0)));

        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(5, 5));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(5, 5));
    }

    [Test]
    public void Merged_Cells_Must_Be_Entirely_Unlocked_To_Be_Selected()
    {
        _sheet.Cells.Merge(new Region(2, 3, 2, 3));
        Unlock(new Region(2, 2));
        Unlock(new Region(8, 8));
        ProtectRestricted();
        _sheet.Selection.Set(8, 8);

        Click(2, 2);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(8, 8));

        _sheet.Protection.RunUnprotected(() => Unlock(new Region(2, 3, 2, 3)));
        _sheet.Selection.Set(8, 8);

        Click(2, 2);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(2, 3, 2, 3));
    }
}
