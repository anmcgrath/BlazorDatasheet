using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
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
    public void Suspended_Dirty_Regions_Survive_A_Later_Batch_And_Flush_On_Resume()
    {
        var sheet = new Sheet(5, 5) { ScreenUpdating = false };
        var captured = Capture(sheet);
        sheet.Cells.SetCellMetaData(0, 0, "status", "ready");
        sheet.BatchUpdates();
        sheet.Cells.SetCellMetaData(3, 3, "status", "ready");
        sheet.EndBatchUpdates();
        captured.Should().BeEmpty();
        sheet.ScreenUpdating = true;
        var dirty = captured.Should().ContainSingle().Subject;
        dirty.Rows.SelectMany(x => Enumerable.Range(x.Start, x.Size)).Should().Contain(new[] { 0, 3 });
        dirty.ColStart.Should().Be(0);
        dirty.ColEnd.Should().Be(3);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Nested_Commands_Preserve_Outer_Update_State(bool screenUpdating)
    {
        var sheet = new Sheet(5, 5) { ScreenUpdating = screenUpdating };
        var captured = Capture(sheet);
        sheet.BatchUpdates();
        sheet.Commands.BeginCommandGroup();
        sheet.Cells.SetValue(1, 1, "value");
        sheet.Rows.InsertAt(0);
        sheet.Cells.SetValues(new Region(3, 4, 3, 4), "bulk");
        sheet.Commands.EndCommandGroup();
        sheet.ScreenUpdating.Should().Be(screenUpdating);
        sheet.Commands.Undo();
        sheet.ScreenUpdating.Should().Be(screenUpdating);
        sheet.Commands.Redo();
        sheet.ScreenUpdating.Should().Be(screenUpdating);
        captured.Should().BeEmpty();
        sheet.EndBatchUpdates();
        if (!screenUpdating) captured.Should().BeEmpty();
        sheet.ScreenUpdating = true;
        captured.Should().ContainSingle();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Failed_Group_Restores_Batching_And_Screen_State(bool throws)
    {
        var sheet = new Sheet(5, 5);
        var group = new CommandGroup(new SetMetaDataCommand(0, 0, "status", "ready"), new FailingCommand(throws));
        if (throws)
            Assert.Throws<InvalidOperationException>(() => sheet.Commands.ExecuteCommand(group));
        else
        {
            sheet.Commands.ExecuteCommand(group).Should().BeFalse();
            sheet.Cells.GetMetaData(0, 0, "status").Should().BeNull();
        }
        sheet.ScreenUpdating.Should().BeTrue();
        var captured = Capture(sheet);
        sheet.Cells.SetCellMetaData(1, 1, "status", "ready");
        captured.Should().ContainSingle();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Group_Undo_Failure_Restores_Caller_Update_State(bool screenUpdating)
    {
        var sheet = new Sheet(3, 3) { ScreenUpdating = screenUpdating };
        var command = new ThrowingUndoCommand();
        sheet.Commands.ExecuteCommand(new CommandGroup(command));
        Assert.Throws<InvalidOperationException>(() => sheet.Commands.Undo());
        sheet.ScreenUpdating.Should().Be(screenUpdating);
        var captured = Capture(sheet);
        sheet.Cells.SetCellMetaData(1, 1, "status", "ready");
        sheet.ScreenUpdating = true;
        captured.Should().ContainSingle();
    }

    private sealed class ThrowingUndoCommand : BaseCommand, IUndoableCommand
    {
        protected override bool CanExecuteCore(Sheet sheet) => true;
        protected override bool ExecuteCore(Sheet sheet) => true;
        public bool Undo(Sheet sheet) => throw new InvalidOperationException();
    }

    private sealed class FailingCommand(bool throws) : BaseCommand
    {
        protected override bool CanExecuteCore(Sheet sheet) => true;
        protected override bool ExecuteCore(Sheet sheet) => throws ? throw new InvalidOperationException() : false;
    }

    [Test]
    public void Failed_Cell_Change_Subscriber_Does_Not_Leak_The_Previous_Batch()
    {
        var sheet = new Sheet(3, 3);
        EventHandler<CellDataChangedEventArgs> handler = (_, _) => throw new InvalidOperationException();
        sheet.Cells.CellsChanged += handler;
        Assert.Throws<InvalidOperationException>(() => sheet.Cells.SetValue(0, 0, "first"));
        sheet.Cells.CellsChanged -= handler;
        sheet.ScreenUpdating.Should().BeTrue();
        var positions = new List<CellPosition>();
        sheet.Cells.CellsChanged += (_, args) => positions.AddRange(args.Positions);
        sheet.Cells.SetValue(1, 1, "second");
        positions.Should().Equal(new CellPosition(1, 1));
    }

    [Test]
    public void Nested_Batch_From_Cell_Change_Subscriber_Flushes_Once()
    {
        var sheet = new Sheet(3, 3);
        var captured = Capture(sheet);
        sheet.Cells.CellsChanged += (_, _) =>
        {
            sheet.BatchUpdates();
            sheet.Cells.SetCellMetaData(2, 2, "status", "ready");
            sheet.EndBatchUpdates();
        };
        sheet.Cells.SetValue(0, 0, "value");
        var dirty = captured.Should().ContainSingle().Subject;
        dirty.Rows.SelectMany(x => Enumerable.Range(x.Start, x.Size)).Should().Contain(new[] { 0, 2 });
        sheet.ScreenUpdating.Should().BeTrue();
    }

    [TestCase("set")]
    [TestCase("null")]
    [TestCase("clear key")]
    [TestCase("clear all")]
    public void Metadata_Write_Undo_And_Redo_Invalidate_Only_The_Target_Cell(string operation)
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetCellMetaData(2, 3, "status", "old");
        sheet.Cells.SetCellMetaData(2, 3, "other", "keep");
        var captured = Capture(sheet);
        var valueChanges = 0;
        sheet.Cells.CellsChanged += (_, _) => valueChanges++;

        switch (operation)
        {
            case "set":
                sheet.Cells.SetCellMetaData(2, 3, "status", "ready");
                break;
            case "null":
                sheet.Cells.SetCellMetaData(2, 3, "status", null);
                break;
            case "clear key":
                sheet.Cells.ClearCellMetaData(2, 3, "status");
                break;
            case "clear all":
                sheet.Cells.ClearCellMetaData(2, 3);
                break;
        }

        captured.Should().ContainSingle();
        sheet.Commands.Undo();
        captured.Should().HaveCount(2);
        sheet.Commands.Redo();
        captured.Should().HaveCount(3);
        foreach (var snapshot in captured)
        {
            snapshot.Rows.Should().ContainSingle().Which.Should().BeEquivalentTo(new Interval(2, 2));
            snapshot.ColStart.Should().Be(3);
            snapshot.ColEnd.Should().Be(3);
        }
        valueChanges.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Region_Metadata_Batches_Dirty_Notifications_For_Execute_Undo_And_Redo(bool outerBatch)
    {
        var sheet = new Sheet(8, 8);
        sheet.ScreenUpdating = false;
        var captured = Capture(sheet);
        var valueChanges = 0;
        sheet.Cells.CellsChanged += (_, _) => valueChanges++;
        var operations = new Action[]
        {
            () => sheet.Cells.SetCellMetaData(new Region(2, 4, 3, 5), "status", "ready"),
            () => sheet.Commands.Undo(),
            () => sheet.Commands.Redo()
        };

        foreach (var operation in operations)
        {
            captured.Clear();
            if (outerBatch)
                sheet.BatchUpdates();

            operation();

            if (outerBatch)
            {
                captured.Should().BeEmpty();
                sheet.EndBatchUpdates();
            }

            captured.Should().BeEmpty();
            sheet.ScreenUpdating.Should().BeFalse();
            sheet.ScreenUpdating = true;
            var snapshot = captured.Should().ContainSingle().Subject;
            snapshot.Rows.SelectMany(interval => Enumerable.Range(interval.Start, interval.Size))
                .Should().BeEquivalentTo(new[] { 2, 3, 4 });
            snapshot.ColStart.Should().Be(3);
            snapshot.ColEnd.Should().Be(5);
            sheet.ScreenUpdating = false;
        }
        valueChanges.Should().Be(0);
    }

    [Test]
    public void Clearing_All_Range_Metadata_Invalidates_The_Range_Once()
    {
        var sheet = new Sheet(5, 5);
        var range = sheet.Range(1, 3, 2, 4);
        range.SetMetaData("status", "ready");
        range.SetMetaData("other", 7);
        var captured = Capture(sheet);

        range.ClearMetaData();

        var snapshot = captured.Should().ContainSingle().Subject;
        snapshot.Rows.SelectMany(interval => Enumerable.Range(interval.Start, interval.Size))
            .Should().BeEquivalentTo(new[] { 1, 2, 3 });
        snapshot.ColStart.Should().Be(2);
        snapshot.ColEnd.Should().Be(4);
    }

    [Test]
    public void Failed_Metadata_Event_Handler_Does_Not_Leave_The_Bulk_Batch_Open()
    {
        var sheet = new Sheet(3, 3);
        EventHandler<CellMetaDataChangeEventArgs> handler =
            (_, _) => throw new InvalidOperationException("Subscriber failed");
        sheet.Cells.MetaDataChanged += handler;

        Action set = () => sheet.Cells.SetCellMetaData(new Region(0, 1, 0, 1), "status", "ready");
        set.Should().Throw<InvalidOperationException>();
        sheet.Cells.MetaDataChanged -= handler;

        var captured = Capture(sheet);
        sheet.Cells.SetCellMetaData(2, 2, "status", "after failure");
        captured.Should().ContainSingle();
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
