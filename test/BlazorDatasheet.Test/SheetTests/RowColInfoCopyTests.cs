using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class RowColInfoCopyTests
{
    private record RowState(
        int Index,
        double Top,
        double VisualHeight,
        double PhysicalHeight,
        bool Visible,
        string? Heading,
        TextAlign? Alignment,
        string? Group,
        string? Background);

    private static List<RowState> Snapshot(Sheet sheet)
    {
        var rows = sheet.Rows;
        return Enumerable.Range(0, sheet.NumRows + 1)
            .Select(i => new RowState(i,
                rows.GetVisualTop(i),
                rows.GetVisualHeight(i),
                rows.GetPhysicalHeight(i),
                rows.IsVisible(i),
                rows.GetHeading(i),
                rows.GetHeadingAlignment(i),
                rows.GetGroup(i)?.Group.Label,
                rows.Formats.Get(i)?.BackgroundColor))
            .ToList();
    }

    private static List<(int, int, string)> Groups(RowColInfoStore store) =>
        store.GetGroups().Select(g => (g.Start, g.End, g.Group.Label)).ToList();

    private static Sheet CreateSheetWithRowInfo()
    {
        var sheet = new Sheet(20, 5);
        sheet.Rows.SetSizesImpl(1, 2, 50);
        sheet.Rows.SetSizesImpl(3, 3, 70);
        sheet.Rows.HideImpl(2, 1);
        sheet.Rows.SetHeadingsImpl(1, 1, "h1");
        sheet.Rows.SetHeadingsImpl(3, 3, "h3");
        sheet.Rows.SetHeadingAlignmentImpl(1, 2, TextAlign.Center);
        sheet.Rows.SetGroupImpl(1, 3, "g");
        sheet.Rows.SetFormatImpl(new CellFormat() { BackgroundColor = "red" }, 2, 3);
        return sheet;
    }

    [Test]
    public void Copy_Rows_Copies_Sizes_Headings_Alignment_Visibility_Groups_And_Formats()
    {
        var sheet = CreateSheetWithRowInfo();
        sheet.Rows.CopyImpl(sheet.Rows, 1, 3, 10);

        sheet.Rows.GetPhysicalHeight(10).Should().Be(50);
        sheet.Rows.GetPhysicalHeight(11).Should().Be(50);
        sheet.Rows.GetPhysicalHeight(12).Should().Be(70);
        sheet.Rows.GetPhysicalHeight(13).Should().Be(24);
        sheet.Rows.GetVisualHeight(10).Should().Be(50);
        sheet.Rows.GetVisualHeight(11).Should().Be(0);
        sheet.Rows.GetVisualHeight(12).Should().Be(70);
        sheet.Rows.IsVisible(11).Should().BeFalse();
        sheet.Rows.IsVisible(10).Should().BeTrue();
        sheet.Rows.IsVisible(12).Should().BeTrue();
        sheet.Rows.GetHeading(10).Should().Be("h1");
        sheet.Rows.GetHeading(11).Should().BeNull();
        sheet.Rows.GetHeading(12).Should().Be("h3");
        sheet.Rows.GetHeadingAlignment(10).Should().Be(TextAlign.Center);
        sheet.Rows.GetHeadingAlignment(11).Should().Be(TextAlign.Center);
        sheet.Rows.GetHeadingAlignment(12).Should().BeNull();
        sheet.Rows.Formats.Get(10).Should().BeNull();
        sheet.Rows.Formats.Get(11)!.BackgroundColor.Should().Be("red");
        sheet.Rows.Formats.Get(12)!.BackgroundColor.Should().Be("red");
        sheet.Rows.Formats.Get(13).Should().BeNull();

        Groups(sheet.Rows).Should().BeEquivalentTo(new List<(int, int, string)>
        {
            (1, 3, "g"),
            (10, 12, "g")
        }, o => o.WithStrictOrdering());

        // the source is untouched
        sheet.Rows.GetPhysicalHeight(1).Should().Be(50);
        sheet.Rows.GetHeading(1).Should().Be("h1");
        sheet.Rows.IsVisible(2).Should().BeFalse();

        // cumulative positions: rows 0..9 = 24 + 50 + 0 + 70 + 6 * 24
        sheet.Rows.GetVisualTop(10).Should().Be(24 + 50 + 0 + 70 + 6 * 24);
        sheet.Rows.GetVisualTop(13).Should().Be(24 + 50 + 0 + 70 + 6 * 24 + 50 + 0 + 70);
        sheet.Rows.GetVisualTop(14).Should().Be(24 + 50 + 0 + 70 + 6 * 24 + 50 + 0 + 70 + 24);
    }

    [Test]
    public void Copy_Rows_Replaces_Existing_Destination_Info()
    {
        var sheet = CreateSheetWithRowInfo();
        sheet.Rows.SetSizesImpl(10, 12, 90);
        sheet.Rows.HideImpl(12, 1);
        sheet.Rows.SetHeadingsImpl(10, 12, "old");
        sheet.Rows.SetGroupImpl(9, 11, "old");
        sheet.Rows.SetFormatImpl(new CellFormat() { BackgroundColor = "blue" }, 10, 12);

        // a source range without any info clears the destination
        sheet.Rows.CopyImpl(sheet.Rows, 15, 3, 10);

        for (int i = 10; i <= 12; i++)
        {
            sheet.Rows.GetPhysicalHeight(i).Should().Be(24);
            sheet.Rows.GetVisualHeight(i).Should().Be(24);
            sheet.Rows.IsVisible(i).Should().BeTrue();
            sheet.Rows.GetHeading(i).Should().BeNull();
            sheet.Rows.GetGroup(i).Should().BeNull();
            sheet.Rows.Formats.Get(i).Should().BeNull();
        }

        // the part of the group outside the destination is kept
        sheet.Rows.GetGroup(9)!.Group.Label.Should().Be("old");
        sheet.Rows.GetVisualTop(13).Should().Be(24 + 50 + 0 + 70 + 6 * 24 + 3 * 24);
    }

    [Test]
    public void Copy_Rows_Restore_Restores_Original_State()
    {
        var sheet = CreateSheetWithRowInfo();
        sheet.Rows.SetSizesImpl(10, 12, 90);
        sheet.Rows.HideImpl(12, 1);
        sheet.Rows.SetHeadingsImpl(11, 12, "old");
        sheet.Rows.SetGroupImpl(9, 11, "old");
        sheet.Rows.SetFormatImpl(new CellFormat() { BackgroundColor = "blue" }, 10, 11);
        var before = Snapshot(sheet);
        var groupsBefore = Groups(sheet.Rows);

        var restoreData = sheet.Rows.CopyImpl(sheet.Rows, 1, 3, 10);
        Snapshot(sheet).Should().NotBeEquivalentTo(before);

        sheet.Rows.Restore(restoreData);
        Snapshot(sheet).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
        Groups(sheet.Rows).Should().BeEquivalentTo(groupsBefore, o => o.WithStrictOrdering());
    }

    [Test]
    [TestCase(1, 4, 3)] // destination overlaps the end of the source
    [TestCase(3, 4, 1)] // destination overlaps the start of the source
    [TestCase(1, 1, 1)] // destination is the source
    public void Copy_Rows_With_Overlapping_Ranges_Copies_Original_Source(int srcStart, int count, int dstStart)
    {
        var sheet = CreateSheetWithRowInfo();
        var before = Snapshot(sheet);

        var restoreData = sheet.Rows.CopyImpl(sheet.Rows, srcStart, count, dstStart);

        var after = Snapshot(sheet);
        for (int i = 0; i < count; i++)
        {
            var expected = before[srcStart + i];
            var actual = after[dstStart + i];
            actual.PhysicalHeight.Should().Be(expected.PhysicalHeight);
            actual.VisualHeight.Should().Be(expected.VisualHeight);
            actual.Visible.Should().Be(expected.Visible);
            actual.Heading.Should().Be(expected.Heading);
            actual.Alignment.Should().Be(expected.Alignment);
            actual.Group.Should().Be(expected.Group);
            actual.Background.Should().Be(expected.Background);
        }

        // cumulative positions agree with the visual heights
        for (int i = 0; i < sheet.NumRows; i++)
            sheet.Rows.GetVisualTop(i + 1).Should().Be(sheet.Rows.GetVisualTop(i) + sheet.Rows.GetVisualHeight(i));

        sheet.Rows.Restore(restoreData);
        Snapshot(sheet).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
    }

    [Test]
    public void Copy_Rows_Across_Sheets_Copies_Info_And_Restores()
    {
        var source = CreateSheetWithRowInfo();
        var target = new Sheet(20, 5);
        target.Rows.SetSizesImpl(0, 19, 30);
        target.Rows.SetHeadingsImpl(5, 6, "t");
        var before = Snapshot(target);
        var sourceBefore = Snapshot(source);

        var restoreData = target.Rows.CopyImpl(source.Rows, 1, 3, 5);

        target.Rows.GetPhysicalHeight(5).Should().Be(50);
        target.Rows.GetPhysicalHeight(6).Should().Be(50);
        target.Rows.GetPhysicalHeight(7).Should().Be(70);
        target.Rows.GetPhysicalHeight(8).Should().Be(30);
        target.Rows.IsVisible(6).Should().BeFalse();
        target.Rows.GetHeading(5).Should().Be("h1");
        target.Rows.GetHeading(6).Should().BeNull();
        target.Rows.GetHeading(7).Should().Be("h3");
        target.Rows.GetHeadingAlignment(5).Should().Be(TextAlign.Center);
        target.Rows.GetGroup(5)!.Group.Label.Should().Be("g");
        target.Rows.Formats.Get(6)!.BackgroundColor.Should().Be("red");
        target.Rows.GetVisualTop(8).Should().Be(5 * 30 + 50 + 0 + 70);
        Groups(target.Rows).Should().BeEquivalentTo(new List<(int, int, string)> { (5, 7, "g") });

        Snapshot(source).Should().BeEquivalentTo(sourceBefore, o => o.WithStrictOrdering());

        target.Rows.Restore(restoreData);
        Snapshot(target).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
        target.Rows.HasGroups.Should().BeFalse();
    }

    [Test]
    public void Copy_Columns_Copies_Widths_Headings_Formats_And_Filters()
    {
        var sheet = new Sheet(5, 20);
        sheet.Columns.SetSizesImpl(2, 2, 200);
        sheet.Columns.HideImpl(3, 1);
        sheet.Columns.SetHeadingsImpl(2, 2, "c2");
        sheet.Columns.SetFormatImpl(new CellFormat() { BackgroundColor = "green" }, 3, 3);
        var filter = new PatternFilter(PatternFilterType.Contains, "x");
        sheet.Columns.Filters.SetImpl(2, [filter]);
        sheet.Columns.Filters.SetImpl(11, [new ValueFilter()]);
        var filtersBefore = sheet.Columns.Filters.GetAll().Select(f => (f.Column, f.Filters.ToList())).ToList();
        var leftBefore = Enumerable.Range(0, 21).Select(sheet.Columns.GetVisualLeft).ToList();

        var restoreData = sheet.Columns.CopyImpl(sheet.Columns, 2, 2, 10);

        sheet.Columns.GetPhysicalWidth(10).Should().Be(200);
        sheet.Columns.GetVisualWidth(11).Should().Be(0);
        sheet.Columns.IsVisible(11).Should().BeFalse();
        sheet.Columns.GetHeading(10).Should().Be("c2");
        sheet.Columns.Formats.Get(11)!.BackgroundColor.Should().Be("green");
        sheet.Columns.Filters.Get(10).Filters.Should().ContainSingle().Which.Should().BeSameAs(filter);
        sheet.Columns.Filters.Get(11).Filters.Should().BeEmpty();
        sheet.Columns.Filters.Get(2).Filters.Should().ContainSingle();
        sheet.Columns.GetVisualLeft(12).Should().Be(105 * 2 + 200 + 0 + 105 * 6 + 200 + 0);

        sheet.Columns.Restore(restoreData);
        sheet.Columns.Filters.GetAll().Select(f => (f.Column, f.Filters.ToList())).Should()
            .BeEquivalentTo(filtersBefore);
        sheet.Columns.Filters.Get(11).Filters.Should().ContainSingle().Which.Should().BeOfType<ValueFilter>();
        sheet.Columns.Filters.Get(10).Filters.Should().BeEmpty();
        Enumerable.Range(0, 21).Select(sheet.Columns.GetVisualLeft).Should()
            .BeEquivalentTo(leftBefore, o => o.WithStrictOrdering());
        sheet.Columns.GetHeading(10).Should().BeNull();
        sheet.Columns.Formats.Get(11).Should().BeNull();
        sheet.Columns.IsVisible(11).Should().BeTrue();
    }

    [Test]
    public void Copy_Columns_Across_Sheets_Copies_Filters()
    {
        var source = new Sheet(5, 10);
        source.Columns.Filters.SetImpl(1, [new ValueFilter()]);
        var target = new Sheet(5, 10);

        var restoreData = target.Columns.CopyImpl(source.Columns, 0, 3, 4);

        target.Columns.Filters.Get(5).Filters.Should().ContainSingle();
        target.Columns.Filters.Get(1).Filters.Should().BeEmpty();
        source.Columns.Filters.Get(1).Filters.Should().ContainSingle();

        target.Columns.Restore(restoreData);
        target.Columns.Filters.GetAll().Should().BeEmpty();
    }

    [Test]
    public void Copy_Emits_Size_Headings_And_Group_Events()
    {
        var sheet = CreateSheetWithRowInfo();
        var sizeModified = false;
        var headingsModified = false;
        var groupsModified = false;
        sheet.Rows.SizeModified += (_, _) => sizeModified = true;
        sheet.Rows.HeadingsModified += (_, _) => headingsModified = true;
        sheet.Rows.GroupsModified += (_, _) => groupsModified = true;

        sheet.Rows.CopyImpl(sheet.Rows, 1, 3, 10);

        sizeModified.Should().BeTrue();
        headingsModified.Should().BeTrue();
        groupsModified.Should().BeTrue();
    }
}
