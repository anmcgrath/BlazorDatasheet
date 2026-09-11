using System;
using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Events.Commands;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SheetProtectionTests
{
    private static Sheet UnlockedSheet()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(sheet.Region, new CellFormat { IsLocked = false });
        return sheet;
    }

    [Test]
    public void Default_Locks_Only_Apply_When_Protected()
    {
        var sheet = new Sheet(5, 5);
        sheet.Protection.IsLocked(0, 0).Should().BeTrue();
        sheet.Cells.SetValue(0, 0, 1).Should().BeTrue();
        sheet.Protection.Protect();
        var rejected = 0;
        sheet.Commands.CommandNotExecuted += (_, _) => rejected++;
        sheet.Cells.SetValue(0, 0, 2).Should().BeFalse();
        new SetCellValueCommand(0, 0, new CellValue(2)).CanExecute(sheet).Should().BeFalse();
        sheet.Cells.SetFormula(0, 0, "=2+3");
        sheet.Cells[0, 0].Value.Should().Be(1);
        rejected.Should().Be(2);
        sheet.Protection.Unprotect();
        sheet.Cells.SetValue(0, 0, 3).Should().BeTrue();
    }

    [Test]
    public void Sparse_Lock_Resolution_Uses_Cell_Column_Row_Precedence()
    {
        var sheet = new Sheet(1_000_000, 1000);
        sheet.SetFormat(new RowRegion(0, 999_999), new CellFormat { IsLocked = false });
        sheet.SetFormat(new ColumnRegion(2), new CellFormat { IsLocked = true });
        sheet.SetFormat(new ColumnRegion(2), new CellFormat { IsLocked = false });
        sheet.SetFormat(new Region(42, 2), new CellFormat { IsLocked = true });
        sheet.Protection.Protect();
        sheet.Protection.CanEdit(new Region(0, 999_999, 3, 999)).Should().BeTrue();
        sheet.Protection.CanEdit(sheet.Region).Should().BeFalse();
        sheet.Protection.IsLocked(42, 2).Should().BeTrue();
        sheet.Protection.IsLocked(43, 2).Should().BeFalse();
    }

    [Test]
    public void Merged_Cell_Requires_Entire_Merge_To_Be_Unlocked()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.Merge(new Region(0, 1, 0, 1));
        sheet.SetFormat(new Region(0, 0), new CellFormat { IsLocked = false });
        sheet.Protection.Protect();
        sheet.Protection.CanEdit(0, 0).Should().BeFalse();
        sheet.Protection.RunUnprotected(() => sheet.SetFormat(new Region(0, 1, 0, 1), new CellFormat { IsLocked = false }));
        sheet.Cells.SetValue(0, 0, "merged").Should().BeTrue();
    }

    [Test]
    public void Bulk_Paste_Clear_And_Command_Chains_Reject_Before_Mutation()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(new Region(0, 0), new CellFormat { IsLocked = false });
        sheet.Cells.SetValue(0, 0, "original");
        sheet.Selection.Set(new Region(2, 2));
        sheet.Protection.Protect();
        sheet.Cells.SetValues(0, 0, new object[][] { new object[] { 1, 2 } }).Should().BeFalse();
        sheet.InsertDelimitedText("1\t2", new CellPosition(0, 0)).Should().BeNull();
        var denied = new SetCellValueCommand(0, 1, new CellValue(9));
        denied.AttachBefore(new ClearCellsCommand(new Region(0, 0)));
        sheet.Commands.ExecuteCommand(denied).Should().BeFalse();
        sheet.Commands.ExecuteCommand(new ClearCellsCommand(new Region(0, 0, 0, 1))).Should().BeFalse();
        sheet.Commands.ExecuteCommand(new CommandGroup(new ClearCellsCommand(new Region(0, 0)), denied)).Should().BeFalse();
        sheet.Cells[0, 0].Value.Should().Be("original");
        sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(2, 2));
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [Test]
    public void Jagged_Writes_Check_Only_Cells_Actually_Written()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(new Region(0, 0, 0, 1), new CellFormat { IsLocked = false });
        sheet.SetFormat(new Region(1, 0), new CellFormat { IsLocked = false });
        sheet.Protection.Protect();
        sheet.Cells.SetValues(0, 0, new object[][] { new object[] { 1, 2 }, new object[] { 3 } }).Should().BeTrue();
        sheet.Cells[1, 1].Value.Should().BeNull();
    }

    [Test]
    public void Appearance_Changes_And_Copy_Preserve_Destination_Locks()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetValue(0, 0, "source");
        sheet.SetFormat(new Region(0, 0), new CellFormat { IsLocked = true, BackgroundColor = "red" });
        sheet.SetFormat(new Region(0, 1), new CellFormat { IsLocked = false });
        sheet.Protection.Protect(new() { AllowFormatCells = true });
        sheet.Commands.ExecuteCommand(new CopyRangeCommand(sheet.Range(0, 0), sheet.Range(0, 1), new())).Should().BeTrue();
        sheet.Cells[0, 1].Value.Should().Be("source");
        sheet.Protection.IsLocked(0, 1).Should().BeFalse();
        sheet.GetFormat(0, 1).BackgroundColor.Should().Be("red");
        sheet.Commands.ExecuteCommand(new ClearFormatCommand(new Region(0, 1))).Should().BeTrue();
        sheet.Protection.IsLocked(0, 1).Should().BeFalse();
        sheet.GetFormat(0, 1).BackgroundColor.Should().BeNull();
        sheet.Commands.ExecuteCommand(new SetFormatCommand(new Region(0, 1), new() { IsLocked = true })).Should().BeFalse();
        sheet.Commands.ExecuteCommand(new SetFormatCommand(new Region(0, 1), new() { IsLocked = null })).Should().BeFalse();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.GetFormat(0, 1).BackgroundColor.Should().Be("red");
        sheet.Protection.IsLocked(0, 1).Should().BeFalse();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Cells[0, 1].Value.Should().BeNull();
        sheet.Protection.IsLocked(0, 1).Should().BeFalse();
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Cells[0, 1].Value.Should().Be("source");
    }

    [Test]
    public void Copy_Checks_Source_Sized_Footprint_And_All_Destinations()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetValues(0, 0, new object[][] { new object[] { 1, 2 } });
        sheet.SetFormat(new Region(1, 0), new CellFormat { IsLocked = false });
        sheet.Protection.Protect();
        sheet.Commands.ExecuteCommand(new CopyRangeCommand(sheet.Range(new Region(0, 0, 0, 1)), sheet.Range(1, 0),
            new CopyOptions { CopyFormat = false })).Should().BeFalse();
        sheet.Commands.ExecuteCommand(new CopyRangeCommand(sheet.Range(0, 0), new[] { sheet.Range(1, 0), sheet.Range(2, 0) },
            new CopyOptions { CopyFormat = false })).Should().BeFalse();
        sheet.Cells[1, 0].Value.Should().BeNull();
    }

    [Test]
    public void Autofill_Expansion_Shrink_And_Dynamic_Formatting_Are_Atomic()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetValue(0, 0, "text");
        sheet.Cells.SetValue(1, 0, "original");
        sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        sheet.SetFormat(new Region(1, 0), new CellFormat { IsLocked = false });
        sheet.Protection.Protect();
        var fill = new AutoFillCommand(new Region(0, 0), new Region(0, 1, 0, 0));
        // Text autofill copies formatting by default. Formatting isn't permitted here, so the
        // fill degrades to a values-only copy rather than doing nothing.
        sheet.Commands.ExecuteCommand(fill).Should().BeTrue();
        sheet.Cells[1, 0].Value.Should().Be("text");
        sheet.GetFormat(1, 0).BackgroundColor.Should().BeNull();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Cells[1, 0].Value.Should().Be("original");
        sheet.Protection.Protect(new() { AllowFormatCells = true });
        sheet.Commands.ExecuteCommand(fill).Should().BeTrue();
        sheet.Cells[1, 0].Value.Should().Be("text");
        sheet.GetFormat(1, 0).BackgroundColor.Should().Be("red");
        sheet.Protection.IsLocked(1, 0).Should().BeFalse();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Cells[1, 0].Value.Should().Be("original");
        // shrinking the fill clears the locked source cell, which is never permitted
        sheet.Commands.ExecuteCommand(new AutoFillCommand(new Region(0, 1, 0, 0), new Region(1, 0))).Should().BeFalse();
        sheet.Cells[0, 0].Value.Should().Be("text");
    }

    [Test]
    public void Sorting_Requires_Permission_And_Unlocked_Data()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValues(0, 0, new object[][] { new object[] { 2 }, new object[] { 1 } });
        var sort = new SortRangeCommand(new Region(0, 1, 0, 0));
        sheet.Protection.Protect();
        sheet.Commands.ExecuteCommand(sort).Should().BeFalse();
        sheet.Protection.Protect(new() { AllowSort = true });
        sheet.Commands.ExecuteCommand(sort).Should().BeTrue();
        sheet.Cells[0, 0].Value.Should().Be(1);
        sheet.Protection.RunUnprotected(() => sheet.SetFormat(new Region(1, 0), new() { IsLocked = true }));
        sheet.Commands.ExecuteCommand(sort).Should().BeFalse();
    }

    [Test]
    public void Row_Operations_Use_Independent_Permissions()
    {
        var sheet = UnlockedSheet();
        sheet.Protection.Protect(new() { AllowInsertRows = true, AllowDeleteRows = true, AllowFormatRows = true });
        sheet.Rows.InsertAt(1);
        sheet.NumRows.Should().Be(6);
        sheet.Columns.InsertAt(1);
        sheet.NumCols.Should().Be(5);
        sheet.Commands.ExecuteCommand(new SetSizeCommand(0, 0, 40, Axis.Row)).Should().BeTrue();
        sheet.Commands.ExecuteCommand(new HideCommand(0, 0, Axis.Col)).Should().BeFalse();
        sheet.Rows.RemoveAt(0).Should().BeTrue();
        sheet.Protection.RunUnprotected(() => sheet.SetFormat(new Region(0, 0), new() { IsLocked = true }));
        sheet.Rows.RemoveAt(0).Should().BeFalse();
        sheet.Columns.RemoveAt(0).Should().BeFalse();
    }

    [Test]
    public void Protected_Formulas_Recalculate_And_Conditional_Formats_Cannot_Unlock()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(new Region(0, 0), new() { IsLocked = false });
        sheet.Cells.SetValue(0, 0, 2);
        sheet.Cells.SetFormula(0, 1, "=A1*2");
        sheet.ConditionalFormats.Apply(new Region(0, 1), new ConditionalFormat((_, _) => true,
            _ => new CellFormat { IsLocked = false }));
        sheet.Protection.Protect();
        sheet.Cells.SetValue(0, 0, 3).Should().BeTrue();
        sheet.Cells[0, 1].Value.Should().Be(6);
        sheet.Protection.CanEdit(0, 1).Should().BeFalse();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Cells[0, 1].Value.Should().Be(4);
    }

    [Test]
    public void Protection_Changes_Clear_Both_Histories_And_Cancel_Locked_Edit()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Commands.Undo();
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.IsEditing.Should().BeTrue();
        sheet.Protection.Protect();
        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Commands.GetRedoCommands().Should().BeEmpty();
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
        sheet.Protection.Unprotect();
        sheet.Commands.Redo().Should().BeFalse();
    }

    [Test]
    public void Nested_Trusted_Updates_Restore_Protection_After_Exception()
    {
        var sheet = new Sheet(5, 5);
        sheet.Protection.Protect();
        Action update = () => sheet.Protection.RunUnprotected(() =>
        {
            sheet.Cells.SetValue(0, 0, 1).Should().BeTrue();
            sheet.Protection.RunUnprotected(() => sheet.Cells.SetValue(0, 1, 2)).Should().BeTrue();
            throw new InvalidOperationException("test");
        });
        update.Should().Throw<InvalidOperationException>();
        sheet.Protection.IsProtected.Should().BeTrue();
        sheet.Cells.SetValue(0, 0, 3).Should().BeFalse();
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
        sheet.Commands.GetRedoCommands().Should().BeEmpty();
    }

    [Test]
    public void Legacy_ReadOnly_Behavior_Is_Preserved()
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(new Region(0, 0), new() { IsReadOnly = true, IsLocked = false });
        sheet.Protection.Protect();
        sheet.Cells.SetValue(0, 0, 1).Should().BeTrue();
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Protection.RunUnprotected(() => sheet.Commands.ExecuteCommand(new ClearCellsCommand(new Region(0, 0))))
            .Should().BeFalse();
    }

    [Test]
    public void Undeclared_Custom_Commands_Are_Rejected_While_Protected()
    {
        var sheet = new Sheet(5, 5);
        var command = new CustomCommand();
        sheet.Commands.ExecuteCommand(command).Should().BeTrue();
        sheet.Protection.Protect();
        sheet.Commands.ExecuteCommand(command).Should().BeFalse();
        sheet.Protection.RunUnprotected(() => sheet.Commands.ExecuteCommand(command)).Should().BeTrue();
    }

    [Test]
    public void Filtering_Locked_Data_Does_Not_Require_Row_Formatting()
    {
        var sheet = new Sheet(3, 1);
        sheet.Cells.SetValue(0, 0, "apple");
        sheet.Cells.SetValue(1, 0, "pear");
        var filter = new PatternFilter(PatternFilterType.StartsWith, "a");
        sheet.Protection.Protect();
        sheet.Columns.Filters.Set(0, filter);
        sheet.Rows.IsVisible(1).Should().BeTrue();
        sheet.Protection.Protect(new() { AllowFilter = true });
        sheet.Columns.Filters.Set(0, filter);
        sheet.Rows.IsVisible(0).Should().BeTrue();
        sheet.Rows.IsVisible(1).Should().BeFalse();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Rows.IsVisible(1).Should().BeTrue();
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Rows.IsVisible(1).Should().BeFalse();
        sheet.Columns.Filters.Clear(0);
        sheet.Rows.IsVisible(1).Should().BeTrue();
    }

    [Test]
    public void Protection_And_Locks_Round_Trip_Through_Json()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValue(0, 0, 3);
        sheet.Protection.Protect(new() { AllowFilter = true, AllowSelectLockedCells = false });
        var json = new BlazorDatasheet.Core.Serialization.Json.SheetJsonSerializer().Serialize(sheet.Workbook);
        var restored = new BlazorDatasheet.Core.Serialization.Json.SheetJsonDeserializer().Deserialize(json).Sheets.First();
        restored.Protection.IsProtected.Should().BeTrue();
        restored.Protection.Options.AllowFilter.Should().BeTrue();
        restored.Protection.Options.AllowSelectLockedCells.Should().BeFalse();
        restored.Protection.CanEdit(0, 0).Should().BeTrue();
        restored.Cells[0, 0].Value.Should().Be(3);
    }

    [Test]
    public void Rejected_Dynamic_Autofill_Preserves_Redo_History()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValue(0, 0, "text");
        sheet.SetFormat(new Region(1, 0), new CellFormat { IsLocked = true });
        sheet.Protection.Protect();
        sheet.Cells.SetValue(2, 0, "later");
        sheet.Commands.Undo();
        var history = sheet.Commands.GetRedoCommands().ToArray();
        sheet.Commands.ExecuteCommand(new AutoFillCommand(new Region(0, 0), new Region(0, 1, 0, 0))).Should().BeFalse();
        sheet.Commands.GetRedoCommands().Should().Equal(history);
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Cells[2, 0].Value.Should().Be("later");
    }

    [Test]
    public void Composite_Redo_Does_Not_Discard_Later_Redo_Entries()
    {
        var sheet = UnlockedSheet();
        sheet.Protection.Protect();
        sheet.Commands.ExecuteCommand(new CommandGroup(
            new SetCellValueCommand(0, 0, new CellValue(1)),
            new SetCellValueCommand(0, 1, new CellValue(2))));
        sheet.Cells.SetValue(0, 2, 3);
        sheet.Commands.Undo();
        sheet.Commands.Undo();
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Cells[0, 2].Value.Should().Be(3);
    }

    [Test]
    public void All_Configuration_Changes_Are_Denied_While_Protected()
    {
        var sheet = UnlockedSheet();
        sheet.Range(0, 0).SetMetaData("key", "value");
        var validator = new BlazorDatasheet.Core.Validation.NumberValidator(false);
        sheet.Range(0, 0).AddValidator(validator);
        sheet.Protection.Protect(new() { AllowFormatCells = true, AllowFormatRows = true, AllowFormatColumns = true });
        sheet.Range(0, 0).ClearMetaData();
        sheet.Cells.GetMetaData(0, 0, "key").Should().Be("value");
        sheet.Validators.Clear(validator, new Region(0, 0));
        sheet.Validators.Get(0, 0).Should().Contain(validator);
        sheet.Range(1, 0).AddValidator(validator);
        sheet.Validators.Get(1, 0).Should().BeEmpty();
        sheet.Cells.Merge(new Region(0, 1, 0, 1));
        sheet.Cells.AnyMerges().Should().BeFalse();
        sheet.Cells.SetType(0, 0, "boolean");
        sheet.Cells.GetCellType(0, 0).Should().NotBe("boolean");
        sheet.Columns.SetGroup(0, 1, "blocked");
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
        sheet.FreezeTopRows(1);
        sheet.FreezeState.Top.Should().Be(1);
    }

    [Test]
    public void Edit_Commit_Rechecks_Protection_After_User_Callback()
    {
        var sheet = new Sheet(5, 5);
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "new";
        sheet.Editor.BeforeEditAccepted += (_, _) => sheet.Protection.Protect();
        sheet.Editor.AcceptEdit().Should().BeFalse();
        sheet.Editor.IsEditing.Should().BeFalse();
        sheet.Cells[0, 0].Value.Should().BeNull();
    }

    [Test]
    public void Multiple_Copy_Destinations_Restore_Independently()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValue(0, 0, "source");
        sheet.Cells.SetValue(1, 0, "first");
        sheet.Cells.SetValue(2, 0, "second");
        sheet.Protection.Protect(new() { AllowFormatCells = true });
        sheet.Commands.ExecuteCommand(new CopyRangeCommand(sheet.Range(0, 0),
            new[] { sheet.Range(1, 0), sheet.Range(2, 0) }, new())).Should().BeTrue();
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Cells[1, 0].Value.Should().Be("first");
        sheet.Cells[2, 0].Value.Should().Be("second");
        sheet.Commands.Redo().Should().BeTrue();
        sheet.Cells[1, 0].Value.Should().Be("source");
        sheet.Cells[2, 0].Value.Should().Be("source");
    }

    [Test]
    public void Dynamic_Autofill_In_A_Group_Is_Checked_Before_Any_Child_Mutates()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValue(0, 0, "source");
        sheet.Cells.SetValue(4, 0, "keep");
        sheet.SetFormat(new Region(1, 0), new CellFormat { IsLocked = true });
        sheet.Protection.Protect();
        var changes = 0;
        sheet.Cells.CellsChanged += (_, _) => changes++;
        var group = new CommandGroup(new ClearCellsCommand(new Region(4, 0)),
            new AutoFillCommand(new Region(0, 0), new Region(0, 1, 0, 0)));
        sheet.Commands.ExecuteCommand(group).Should().BeFalse();
        changes.Should().Be(0);
        sheet.Cells[4, 0].Value.Should().Be("keep");
    }

    [Test]
    public void Dynamic_Autofill_In_A_Group_Fills_From_The_Values_The_Group_Wrote()
    {
        var sheet = UnlockedSheet();
        sheet.Protection.Protect(new() { AllowFormatCells = true });
        sheet.Commands.ExecuteCommand(new CommandGroup(
            new SetCellValueCommand(0, 0, new CellValue(1)),
            new SetCellValueCommand(1, 0, new CellValue(2)),
            new AutoFillCommand(new Region(0, 1, 0, 0), new Region(0, 3, 0, 0)))).Should().BeTrue();
        sheet.Cells[2, 0].Value.Should().Be(3);
        sheet.Cells[3, 0].Value.Should().Be(4);
    }

    [Test]
    public void Rejected_Autofill_Does_Not_Replay_Stale_Commands_Later()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Cells.SetValue(1, 0, 2);
        sheet.SetFormat(new Region(2, 3, 0, 0), new CellFormat { IsLocked = true });
        sheet.Protection.Protect();

        var fill = new AutoFillCommand(new Region(0, 1, 0, 0), new Region(0, 3, 0, 0));
        sheet.Commands.ExecuteCommand(fill).Should().BeFalse();

        sheet.Protection.Unprotect();
        sheet.Cells.SetValue(0, 0, 10);
        sheet.Cells.SetValue(1, 0, 20);
        sheet.Commands.ExecuteCommand(fill).Should().BeTrue();
        sheet.Cells[2, 0].Value.Should().Be(30);
        sheet.Cells[3, 0].Value.Should().Be(40);
    }

    [Test]
    public void Dynamic_Autofill_In_A_Group_Run_From_A_Handler_Is_Checked_Before_Any_Child_Mutates()
    {
        var sheet = UnlockedSheet();
        sheet.Cells.SetValue(0, 0, "source");
        sheet.Cells.SetValue(4, 0, "keep");
        sheet.SetFormat(new Region(1, 0), new CellFormat { IsLocked = true });
        sheet.Protection.Protect();

        var changes = 0;
        sheet.Cells.CellsChanged += (_, _) => changes++;

        bool? groupResult = null;
        var changesDuringGroup = 0;
        EventHandler<CommandRunEventArgs>? handler = null;
        handler = (_, _) =>
        {
            // run the group re-entrantly, from inside another command's execution
            sheet.Commands.CommandRun -= handler;
            var changesBefore = changes;
            groupResult = sheet.Commands.ExecuteCommand(new CommandGroup(
                new ClearCellsCommand(new Region(4, 0)),
                new AutoFillCommand(new Region(0, 0), new Region(0, 1, 0, 0))));
            changesDuringGroup = changes - changesBefore;
        };
        sheet.Commands.CommandRun += handler;

        sheet.Cells.SetValue(3, 0, "trigger").Should().BeTrue();

        groupResult.Should().BeFalse();
        changesDuringGroup.Should().Be(0);
        sheet.Cells[4, 0].Value.Should().Be("keep");
    }

    private sealed class CustomCommand : BaseCommand
    {
        protected override bool CanExecuteCore(Sheet sheet) => true;
        protected override bool ExecuteCore(Sheet sheet) => true;
    }
}
