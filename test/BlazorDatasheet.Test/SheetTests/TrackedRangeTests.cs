using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class TrackedRangeTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet(20, 20);
    }

    private IRegion RegionOf(string key) => _workbook.TrackedRanges.Get(key)!.Region;

    [Test]
    public void Tracked_Range_Reads_Back_Where_It_Was_Tracked()
    {
        var range = _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        range.Key.Should().Be("a");
        range.SheetName.Should().Be(_sheet.Name);
        range.Region.Should().BeEquivalentTo(new Region(3, 1));
        range.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void Insert_Rows_At_Or_Before_Range_Moves_It_And_Undo_Restores_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _sheet.Rows.InsertAt(3, 2);
        RegionOf("a").Should().BeEquivalentTo(new Region(5, 1));

        _sheet.Commands.Undo();
        RegionOf("a").Should().BeEquivalentTo(new Region(3, 1));

        _sheet.Commands.Redo();
        RegionOf("a").Should().BeEquivalentTo(new Region(5, 1));
    }

    [Test]
    public void Insert_Columns_Before_Range_Moves_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _sheet.Columns.InsertAt(0);

        RegionOf("a").Should().BeEquivalentTo(new Region(3, 2));
    }

    [Test]
    public void Insert_Rows_Inside_Range_Grows_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(2, 5, 1, 1));

        _sheet.Rows.InsertAt(3, 2);

        RegionOf("a").Should().BeEquivalentTo(new Region(2, 7, 1, 1));
    }

    [Test]
    public void Insert_Rows_Just_After_Range_Leaves_It_Alone()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(2, 5, 1, 1));

        _sheet.Rows.InsertAt(6);

        RegionOf("a").Should().BeEquivalentTo(new Region(2, 5, 1, 1));
    }

    [Test]
    public void Remove_Rows_Before_Range_Moves_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(10, 1));

        _sheet.Rows.RemoveAt(2, 3);

        RegionOf("a").Should().BeEquivalentTo(new Region(7, 1));
    }

    [Test]
    public void Remove_Rows_Containing_Cell_Deletes_It_Keeping_Last_Region_And_Undo_Restores_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _sheet.Rows.RemoveAt(2, 3);

        var deleted = _workbook.TrackedRanges.Get("a")!;
        deleted.IsDeleted.Should().BeTrue();
        deleted.Region.Should().BeEquivalentTo(new Region(3, 1));

        _sheet.Commands.Undo();
        var restored = _workbook.TrackedRanges.Get("a")!;
        restored.IsDeleted.Should().BeFalse();
        restored.Region.Should().BeEquivalentTo(new Region(3, 1));

        _sheet.Commands.Redo();
        _workbook.TrackedRanges.Get("a")!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public void Remove_Columns_Containing_Cell_Deletes_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _sheet.Columns.RemoveAt(1);

        _workbook.TrackedRanges.Get("a")!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public void Remove_Rows_Overlapping_Top_Of_Range_Shrinks_And_Moves_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 10, 1, 1));

        _sheet.Rows.RemoveAt(2, 3);

        RegionOf("a").Should().BeEquivalentTo(new Region(2, 7, 1, 1));
        _sheet.Commands.Undo();
        RegionOf("a").Should().BeEquivalentTo(new Region(3, 10, 1, 1));
    }

    [Test]
    public void Remove_Rows_Overlapping_Bottom_Of_Range_Shrinks_It()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(0, 3, 1, 1));

        _sheet.Rows.RemoveAt(2, 3);

        RegionOf("a").Should().BeEquivalentTo(new Region(0, 1, 1, 1));
    }

    [Test]
    public void Range_Beyond_Sheet_Extent_Is_Kept_And_Moves()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(100, 30));

        _sheet.Rows.InsertAt(0);

        RegionOf("a").Should().BeEquivalentTo(new Region(101, 30));
    }

    [Test]
    public void Range_Follows_Sheet_Rename()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _workbook.RenameSheet(_sheet.Name, "Renamed");

        _workbook.TrackedRanges.Get("a")!.SheetName.Should().Be("Renamed");
    }

    [Test]
    public void Removing_Sheet_Detaches_Its_Ranges_Keeping_Their_Region()
    {
        var other = _workbook.AddSheet(5, 5);
        _workbook.TrackedRanges.Track("a", other.Name, new Region(3, 1));

        _workbook.RemoveSheet(other.Name);

        _workbook.TrackedRanges.Get("a").Should().BeEquivalentTo(
            new TrackedRange("a", other.Name, new Region(3, 1), false));
    }

    [Test]
    public void Range_Tracked_On_Missing_Sheet_Attaches_When_The_Sheet_Is_Added()
    {
        _workbook.TrackedRanges.Track("a", "Output", new Region(3, 1));
        _workbook.TrackedRanges.Get("a")!.SheetName.Should().Be("Output");

        var output = _workbook.AddSheet("Output", 10, 10);
        output.Rows.InsertAt(0);

        RegionOf("a").Should().BeEquivalentTo(new Region(4, 1));
    }

    [Test]
    public void Range_Tracked_On_Missing_Sheet_Attaches_When_A_Sheet_Is_Renamed_To_It()
    {
        _workbook.TrackedRanges.Track("a", "Output", new Region(3, 1));

        _workbook.RenameSheet(_sheet.Name, "Output");
        _sheet.Rows.InsertAt(0);

        RegionOf("a").Should().BeEquivalentTo(new Region(4, 1));
    }

    [TestCase("Track")]
    [TestCase("Get")]
    [TestCase("GetAll")]
    public void Mutating_Detached_Snapshot_Does_Not_Change_Tracked_Region(string source)
    {
        var tracked = _workbook.TrackedRanges.Track("a", "Output", new Region(3, 1));
        var snapshot = source switch
        {
            "Track" => tracked,
            "Get" => _workbook.TrackedRanges.Get("a")!,
            _ => _workbook.TrackedRanges.GetAll().Single()
        };

        snapshot.Region.Shift(10, 0);

        RegionOf("a").Should().BeEquivalentTo(new Region(3, 1));
        _workbook.AddSheet("Output", 20, 20);
        RegionOf("a").Should().BeEquivalentTo(new Region(3, 1));
    }

    [Test]
    public void Detached_Range_Does_Not_Move_With_Other_Sheets()
    {
        _workbook.TrackedRanges.Track("a", "Output", new Region(3, 1));

        _sheet.Rows.InsertAt(0);

        RegionOf("a").Should().BeEquivalentTo(new Region(3, 1));
    }

    [Test]
    public void Tracking_A_Key_Again_Replaces_It_On_Any_Sheet()
    {
        var other = _workbook.AddSheet(5, 5);
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _workbook.TrackedRanges.Track("a", other.Name, new Region(0, 0));

        _workbook.TrackedRanges.GetAll().Should().ContainSingle();
        _workbook.TrackedRanges.Get("a")!.SheetName.Should().Be(other.Name);
    }

    [Test]
    public void Untrack_Removes_Range()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _workbook.TrackedRanges.Untrack("a").Should().BeTrue();

        _workbook.TrackedRanges.Get("a").Should().BeNull();
        _workbook.TrackedRanges.Untrack("a").Should().BeFalse();
    }

    [Test]
    public void Undo_Does_Not_Bring_Back_A_Range_Untracked_Since()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));
        _sheet.Rows.RemoveAt(2, 3);
        _workbook.TrackedRanges.Untrack("a");

        _sheet.Commands.Undo();

        _workbook.TrackedRanges.Get("a").Should().BeNull();
    }

    [Test]
    public void Undo_Does_Not_Duplicate_A_Range_Tracked_Again_Since()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));
        _sheet.Rows.RemoveAt(2, 3);
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(0, 0));

        _sheet.Commands.Undo();

        var range = _workbook.TrackedRanges.Get("a")!;
        range.IsDeleted.Should().BeFalse();
        range.Region.Should().BeEquivalentTo(new Region(0, 0));
        _workbook.TrackedRanges.GetAll().Should().ContainSingle();
    }

    [Test]
    public void Tracked_Ranges_Round_Trip_Through_Serialization_Including_Deleted_Ones()
    {
        var other = _workbook.AddSheet(5, 5);
        _workbook.TrackedRanges.Track("live", _sheet.Name, new Region(2, 5, 1, 3));
        _workbook.TrackedRanges.Track("deleted", _sheet.Name, new Region(10, 1));
        _workbook.TrackedRanges.Track("detached", other.Name, new Region(1, 1));
        _sheet.Rows.RemoveAt(10);
        _workbook.RemoveSheet(other.Name);

        var json = new SheetJsonSerializer().Serialize(_workbook);
        var restored = new SheetJsonDeserializer().Deserialize(json);

        restored.TrackedRanges.Get("live").Should().BeEquivalentTo(
            new TrackedRange("live", _sheet.Name, new Region(2, 5, 1, 3), false));
        restored.TrackedRanges.Get("deleted").Should().BeEquivalentTo(
            new TrackedRange("deleted", _sheet.Name, new Region(10, 1), true));
        restored.TrackedRanges.Get("detached").Should().BeEquivalentTo(
            new TrackedRange("detached", other.Name, new Region(1, 1), false));

        // A deleted range stays deleted: a later insert does not move it back into play.
        restored.GetSheet(_sheet.Name)!.Rows.InsertAt(0);
        restored.TrackedRanges.Get("deleted")!.Region.Should().BeEquivalentTo(new Region(10, 1));
    }

    [Test]
    public void Workbook_Without_Tracked_Ranges_Serializes_Without_The_Section()
    {
        var json = new SheetJsonSerializer().Serialize(_workbook);

        json.Should().NotContain("TrackedRanges");
    }

    [Test]
    public void Duplicating_A_Sheet_Does_Not_Copy_Its_Ranges()
    {
        _workbook.TrackedRanges.Track("a", _sheet.Name, new Region(3, 1));

        _workbook.DuplicateSheet(_sheet.Name, "Copy");

        _workbook.TrackedRanges.GetAll().Single().SheetName.Should().Be(_sheet.Name);
    }
}
