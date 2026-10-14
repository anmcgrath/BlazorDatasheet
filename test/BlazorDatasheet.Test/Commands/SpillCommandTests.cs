using System.Linq;
using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SpillCommandTests
{
    private static object?[] Column(Sheet sheet, int col, int start, int count) =>
        Enumerable.Range(start, count).Select(row => sheet.Cells.GetValue(row, col)).ToArray();

    [TestCase(0, 2)]
    [TestCase(0, 0)]
    [TestCase(1, 2)]
    public void Sort_Intersecting_A_Spill_Is_Rejected_Without_Changing_History(int top, int bottom)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "={3;2;1}");
        var command = new SortRangeCommand(new Region(top, bottom, 0, 0));

        command.CanExecute(sheet).Should().BeFalse();
        command.Execute(sheet).Should().BeFalse();
        sheet.Commands.ExecuteCommand(command).Should().BeFalse();
        Column(sheet, 0, 0, 5).Should().Equal(3, 2, 1, null, null);
        sheet.Cells.IsSpilledCell(1, 0).Should().BeTrue();

        sheet.Commands.Undo();
        Column(sheet, 0, 0, 3).Should().Equal(new object?[] { null, null, null });
        sheet.Commands.Redo();
        Column(sheet, 0, 0, 3).Should().Equal(3, 2, 1);
    }

    [Test]
    public void Sort_Beside_A_Spill_Remains_Undoable()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "={3;2;1}");
        sheet.Cells.SetValues(0, 1, new object[][] { new object[] { 3 }, new object[] { 1 }, new object[] { 2 } });

        sheet.Commands.ExecuteCommand(new SortRangeCommand(new Region(0, 2, 1, 1))).Should().BeTrue();
        Column(sheet, 1, 0, 3).Should().Equal(1, 2, 3);
        sheet.Commands.Undo();
        Column(sheet, 1, 0, 3).Should().Equal(3, 1, 2);
        Column(sheet, 0, 0, 3).Should().Equal(3, 2, 1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Copying_Descendants_Without_Their_Anchor_Copies_Values_And_Undoes(bool valuesOnly)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "={1;2;3}");
        sheet.Cells.SetValue(1, 2, "old");
        var options = valuesOnly ? CopyOptions.ValuesOnly : new CopyOptions();
        var command = new CopyRangeCommand(sheet.Range(new Region(1, 2, 0, 0)),
            sheet.Range(new Region(1, 2, 2, 2)), options);

        sheet.Commands.ExecuteCommand(command).Should().BeTrue();
        Column(sheet, 2, 1, 2).Should().Equal(2, 3);
        sheet.Cells.IsSpilledCell(1, 2).Should().BeFalse();
        sheet.Commands.Undo();
        Column(sheet, 2, 1, 2).Should().Equal(new object?[] { "old", null });
        sheet.Commands.Redo();
        Column(sheet, 2, 1, 2).Should().Equal(2, 3);
    }

    [TestCase(false, PasteMode.All)]
    [TestCase(true, PasteMode.All)]
    [TestCase(false, PasteMode.Values)]
    [TestCase(true, PasteMode.Values)]
    public void Snapshot_Paste_Preserves_Spills_Or_Static_Values_According_To_Mode(bool html, PasteMode mode)
    {
        var source = new Sheet(10, 10);
        source.Cells.SetFormula(0, 0, "={1;2;3}");
        var snapshot = ClipboardSnapshotBuilder.FromRange(source.Range(new Region(0, 2, 0, 0)));
        if (html)
            snapshot = ClipboardHtmlParser.Parse(ClipboardHtmlWriter.Write(snapshot))!;
        var target = new Sheet(10, 10);
        target.Cells.SetValue(2, 2, "old");

        target.Commands.ExecuteCommand(new PasteSnapshotCommand(snapshot, new CellPosition(2, 2), mode))
            .Should().BeTrue();
        Column(target, 2, 2, 3).Should().Equal(1, 2, 3);
        target.Cells.IsSpilledCell(3, 2).Should().Be(mode == PasteMode.All);
        target.Cells.HasFormula(2, 2).Should().Be(mode == PasteMode.All);

        target.Commands.Undo();
        Column(target, 2, 2, 3).Should().Equal(new object?[] { "old", null, null });
        target.Commands.Redo();
        Column(target, 2, 2, 3).Should().Equal(1, 2, 3);
        target.Cells.IsSpilledCell(4, 2).Should().Be(mode == PasteMode.All);
    }

    [Test]
    public void Snapshot_Of_Descendants_Without_Their_Anchor_Pastes_Literal_Values()
    {
        var source = new Sheet(10, 10);
        source.Cells.SetFormula(0, 0, "={1;2;3}");
        var snapshot = ClipboardSnapshotBuilder.FromRange(source.Range(new Region(1, 2, 0, 0)));
        snapshot = ClipboardHtmlParser.Parse(ClipboardHtmlWriter.Write(snapshot))!;
        var target = new Sheet(10, 10);

        target.Commands.ExecuteCommand(new PasteSnapshotCommand(snapshot, new CellPosition(0, 0)))
            .Should().BeTrue();

        Column(target, 0, 0, 2).Should().Equal(2, 3);
        target.Cells.HasFormula(0, 0).Should().BeFalse();
        target.Cells.IsSpilledCell(1, 0).Should().BeFalse();
    }

    [Test]
    public void Formats_Only_Copy_Of_A_Merge_Blocks_A_Spill_And_Undo_Rebuilds_It()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.Merge(new Region(1, 1, 2, 3));
        sheet.Cells.SetFormula(0, 0, "={1;2;3}");
        sheet.Commands.ExecuteCommand(new CopyRangeCommand(sheet.Range(new Region(1, 1, 2, 3)),
            sheet.Range(new Region(1, 1, 0, 1)), CopyOptions.FormatsOnly)).Should().BeTrue();
        sheet.Cells.IsSpillAnchor(0, 0).Should().BeFalse();
        sheet.Cells.GetValue(1, 0).Should().BeNull();

        sheet.Commands.Undo();
        Column(sheet, 0, 0, 3).Should().Equal(1, 2, 3);
        sheet.Cells.IsSpilledCell(2, 0).Should().BeTrue();
        sheet.Commands.Redo();
        sheet.Cells.IsSpillAnchor(0, 0).Should().BeFalse();
        sheet.Cells.GetValue(1, 0).Should().BeNull();
    }
}
