using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class RegionDataStoreCellShiftTests
{
    private static IRegion RegionOf<T>(RegionDataStore<T> store, T data) where T : System.IEquatable<T> =>
        store.GetAllDataRegions().Single(x => x.Data.Equals(data)).Region;

    private static string[] Snapshot(RegionDataStore<string> store) =>
        store.GetAllDataRegions()
            .Select(x => $"{x.Data}:{x.Region.Top},{x.Region.Bottom},{x.Region.Left},{x.Region.Right}")
            .OrderBy(x => x).ToArray();

    [Test]
    public void Insert_Shift_Right_Moves_Region_In_Band_After_Index()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(1, 2, 3, 4), "a");
        var restore = store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(1, 2, 5, 6));
        restore.Shifts.Should().BeNull();
        restore.RegionsAdded.Should().ContainSingle();
        restore.RegionsRemoved.Should().ContainSingle();
    }

    [Test]
    public void Insert_Shift_Down_Moves_Region_In_Band_After_Index()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(3, 4, 1, 2), "a");
        store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Row);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(5, 6, 1, 2));
    }

    [Test]
    public void Insert_Expands_Region_Spanning_Index()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(1, 2, 0, 3), "a");
        store.InsertCellsAt(new Region(1, 2, 2, 3), Axis.Col);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(1, 2, 0, 5));
    }

    [TestCase(true, 5)]
    [TestCase(false, 3)]
    public void Insert_After_Trailing_Edge_Respects_ExpandWhenInsertAfter(bool expand, int expectedRight)
    {
        var store = new RegionDataStore<string>(expandWhenInsertAfter: expand);
        store.Add(new Region(1, 2, 0, 3), "a");
        var restore = store.InsertCellsAt(new Region(1, 2, 4, 5), Axis.Col);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(1, 2, 0, expectedRight));
        if (!expand)
            restore.RegionsAdded.Should().BeEmpty();
    }

    [Test]
    public void Insert_Leaves_Regions_Outside_Band_And_Before_Index()
    {
        var store = new RegionDataStore<string>(expandWhenInsertAfter: false);
        store.Add(new Region(0, 0, 3, 4), "above");
        store.Add(new Region(1, 2, 0, 0), "before");
        var restore = store.InsertCellsAt(new Region(1, 2, 2, 2), Axis.Col);
        RegionOf(store, "above").Should().BeEquivalentTo(new Region(0, 0, 3, 4));
        RegionOf(store, "before").Should().BeEquivalentTo(new Region(1, 2, 0, 0));
        restore.RegionsAdded.Should().BeEmpty();
        restore.RegionsRemoved.Should().BeEmpty();
    }

    [Test]
    public void Insert_Splits_Straddling_Region()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 3, 2, 2), "a");
        store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col, StraddlePolicy.Split);
        store.GetAllDataRegions().Select(x => x.Region).Should().BeEquivalentTo(new IRegion[]
        {
            new Region(0, 0, 2, 2),
            new Region(3, 3, 2, 2),
            new Region(1, 2, 4, 4)
        });
    }

    [Test]
    public void Insert_Keeps_Straddling_Region()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 3, 2, 2), "a");
        var restore = store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col, StraddlePolicy.Keep);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(0, 3, 2, 2));
        restore.RegionsAdded.Should().BeEmpty();
        restore.RegionsRemoved.Should().BeEmpty();
    }

    [Test]
    public void Insert_Splits_Column_Region_Straddling_Band()
    {
        var store = new RegionDataStore<string>();
        store.Add(new ColumnRegion(3), "a");
        store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col);
        store.GetData(0, 3).Should().Contain("a");
        store.GetData(10, 3).Should().Contain("a");
        store.GetData(1, 3).Should().BeEmpty();
        store.GetData(1, 5).Should().Contain("a");
    }

    [Test]
    public void Whole_Row_And_All_Regions_Are_Untouched_By_Band_Shift()
    {
        var store = new RegionDataStore<string>();
        store.Add(new AllRegion(), "all");
        store.Add(new RowRegion(1, 2), "rows");
        var restore = store.InsertCellsAt(new Region(1, 2, 0, 2), Axis.Col);
        restore.RegionsAdded.Should().BeEmpty();
        RegionOf(store, "all").Should().BeOfType<AllRegion>();
        RegionOf(store, "rows").Should().BeOfType<RowRegion>();

        restore = store.RemoveCellsAt(new Region(1, 2, 0, 2), Axis.Col);
        restore.RegionsAdded.Should().BeEmpty();
        restore.RegionsRemoved.Should().BeEmpty();
    }

    [Test]
    public void Insert_Does_Not_Overflow_Region_To_Max()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(1, 2, 3, int.MaxValue), "a");
        store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(1, 2, 5, int.MaxValue));
    }

    [Test]
    public void Remove_Deletes_Contracts_And_Shifts()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(1, 2, 2, 3), "inside");
        store.Add(new Region(1, 2, 0, 2), "overlapLeft");
        store.Add(new Region(1, 2, 3, 5), "overlapRight");
        store.Add(new Region(1, 2, 6, 7), "after");
        store.Add(new Region(0, 0, 6, 7), "outside");

        var restore = store.RemoveCellsAt(new Region(1, 2, 2, 3), Axis.Col);

        store.GetAllDataRegions().Any(x => x.Data == "inside").Should().BeFalse();
        RegionOf(store, "overlapLeft").Should().BeEquivalentTo(new Region(1, 2, 0, 1));
        RegionOf(store, "overlapRight").Should().BeEquivalentTo(new Region(1, 2, 2, 4));
        RegionOf(store, "after").Should().BeEquivalentTo(new Region(1, 2, 4, 5));
        RegionOf(store, "outside").Should().BeEquivalentTo(new Region(0, 0, 6, 7));
        restore.Shifts.Should().BeNull();
    }

    [Test]
    public void Remove_Shift_Up_Contracts_Spanning_Region()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 4, 1, 1), "a");
        store.RemoveCellsAt(new Region(1, 2, 1, 2), Axis.Row);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(0, 2, 1, 1));
    }

    [Test]
    public void Remove_Split_Drops_In_Band_Piece_Keeps_Outside()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 3, 2, 2), "a");
        store.RemoveCellsAt(new Region(1, 2, 2, 2), Axis.Col);
        store.GetAllDataRegions().Select(x => x.Region).Should().BeEquivalentTo(new IRegion[]
        {
            new Region(0, 0, 2, 2),
            new Region(3, 3, 2, 2)
        });
    }

    [Test]
    public void Remove_Keep_Leaves_Straddling_Region()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 3, 2, 2), "a");
        store.RemoveCellsAt(new Region(1, 2, 2, 2), Axis.Col, StraddlePolicy.Keep);
        RegionOf(store, "a").Should().BeEquivalentTo(new Region(0, 3, 2, 2));
    }

    [Test]
    public void Remove_Respects_Min_Area()
    {
        var store = new RegionDataStore<string>(minArea: 2);
        store.Add(new Region(1, 1, 0, 2), "a");
        store.RemoveCellsAt(new Region(1, 1, 1, 2), Axis.Col);
        store.GetAllDataRegions().Should().BeEmpty();
    }

    [TestCase(Axis.Col, true)]
    [TestCase(Axis.Row, true)]
    [TestCase(Axis.Col, false)]
    [TestCase(Axis.Row, false)]
    public void Cell_Shift_Restores(Axis axis, bool insert)
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 4, 2, 2), "straddle");
        store.Add(new Region(2, 2, 0, 4), "straddle2");
        store.Add(new Region(1, 2, 1, 1), "inside");
        store.Add(new Region(3, 3, 3, 3), "after");
        store.Add(new Region(0, 3, 0, 3), "big");
        var before = Snapshot(store);

        var region = new Region(1, 2, 1, 2);
        var restore = insert ? store.InsertCellsAt(region, axis) : store.RemoveCellsAt(region, axis);
        Snapshot(store).Should().NotBeEquivalentTo(before);
        restore.Shifts.Should().BeNull();

        store.Restore(restore);
        Snapshot(store).Should().BeEquivalentTo(before);
    }

    [Test]
    public void Two_Cell_Shifts_Restore_In_Reverse()
    {
        var store = new RegionDataStore<string>();
        store.Add(new Region(0, 4, 2, 2), "a");
        store.Add(new Region(1, 3, 1, 3), "b");
        var before = Snapshot(store);
        var r1 = store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col);
        var r2 = store.RemoveCellsAt(new Region(0, 3, 3, 3), Axis.Row);
        store.Restore(r2);
        store.Restore(r1);
        Snapshot(store).Should().BeEquivalentTo(before);
    }

    [Test]
    public void Consolidated_Store_Cell_Shift_Restores()
    {
        var store = new ConsolidatedDataStore<string>();
        store.Add(new Region(0, 4, 2, 2), "a");
        store.Add(new Region(1, 1, 3, 4), "b");
        var before = Snapshot(store);
        var restore = store.InsertCellsAt(new Region(1, 2, 1, 1), Axis.Col);
        store.GetData(1, 3).Should().Contain("a");
        store.GetData(0, 2).Should().Contain("a");
        store.GetData(1, 5).Should().Contain("b");
        store.Restore(restore);
        Snapshot(store).Should().BeEquivalentTo(before);
    }
}
