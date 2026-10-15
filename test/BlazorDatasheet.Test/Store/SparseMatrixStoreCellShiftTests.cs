using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class SparseMatrixStoreCellShiftTests
{
    /// <summary>
    /// Fills a 5x5 grid (rows/cols 0-4) with "r,c" strings.
    /// </summary>
    private static SparseMatrixStoreByRows<string> GetGrid()
    {
        var store = new SparseMatrixStoreByRows<string>();
        for (int r = 0; r < 5; r++)
        for (int c = 0; c < 5; c++)
            store.Set(r, c, $"{r},{c}");
        return store;
    }

    private static List<(int row, int col, string data)> Snapshot(IMatrixDataStore<string> store) =>
        store.GetNonEmptyData(new Region(0, 100, 0, 100)).OrderBy(x => x.row).ThenBy(x => x.col).ToList();

    [Test]
    public void Insert_Cells_Shift_Right_Moves_Only_Band()
    {
        var store = GetGrid();
        store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Col);

        // in band: col 0 stays, cols >= 1 move right by 2
        store.Get(1, 0).Should().Be("1,0");
        store.Get(1, 1).Should().BeNull();
        store.Get(1, 2).Should().BeNull();
        store.Get(1, 3).Should().Be("1,1");
        store.Get(2, 6).Should().Be("2,4");
        // outside band unchanged
        store.Get(0, 1).Should().Be("0,1");
        store.Get(3, 1).Should().Be("3,1");
        store.Get(0, 5).Should().BeNull();
    }

    [Test]
    public void Insert_Cells_Shift_Down_Moves_Only_Band()
    {
        var store = GetGrid();
        store.InsertCellsAt(new Region(1, 2, 1, 2), Axis.Row);

        store.Get(0, 1).Should().Be("0,1");
        store.Get(1, 1).Should().BeNull();
        store.Get(2, 2).Should().BeNull();
        store.Get(3, 1).Should().Be("1,1");
        store.Get(6, 2).Should().Be("4,2");
        // columns outside the band keep their values, rows 5-6 only have band data
        store.Get(3, 0).Should().Be("3,0");
        store.Get(3, 3).Should().Be("3,3");
        store.Get(5, 0).Should().BeNull();
        store.Get(5, 3).Should().BeNull();
        store.GetNextNonBlankRow(4, 1).Should().Be(5);
    }

    [Test]
    public void Remove_Cells_Shift_Left_Returns_Removed_And_Shifts_Band()
    {
        var store = GetGrid();
        var restore = store.RemoveCellsAt(new Region(1, 2, 1, 2), Axis.Col);

        restore.DataRemoved.Select(x => x.data).Should()
            .BeEquivalentTo(["1,1", "1,2", "2,1", "2,2"]);
        store.Get(1, 0).Should().Be("1,0");
        store.Get(1, 1).Should().Be("1,3");
        store.Get(2, 2).Should().Be("2,4");
        store.Get(2, 3).Should().BeNull();
        store.Get(0, 1).Should().Be("0,1");
        store.Get(3, 4).Should().Be("3,4");
    }

    [Test]
    public void Remove_Cells_Shift_Up_Returns_Removed_And_Shifts_Band()
    {
        var store = GetGrid();
        var restore = store.RemoveCellsAt(new Region(1, 2, 1, 2), Axis.Row);

        restore.DataRemoved.Select(x => x.data).Should()
            .BeEquivalentTo(["1,1", "1,2", "2,1", "2,2"]);
        store.Get(1, 1).Should().Be("3,1");
        store.Get(2, 2).Should().Be("4,2");
        store.Get(3, 1).Should().BeNull();
        store.Get(4, 2).Should().BeNull();
        store.Get(1, 0).Should().Be("1,0");
        store.Get(1, 3).Should().Be("1,3");
    }

    [Test]
    public void Remove_Cells_Drops_Emptied_Rows()
    {
        var store = new SparseMatrixStoreByRows<string>();
        store.Set(3, 1, "a");
        store.RemoveCellsAt(new Region(1, 2, 1, 1), Axis.Row);
        store.Get(1, 1).Should().Be("a");
        store.GetNextNonEmptyIndex(1).Should().Be(-1);
        store.GetNextNonEmptyIndex(0).Should().Be(1);
    }

    [TestCase(Axis.Col, true)]
    [TestCase(Axis.Row, true)]
    [TestCase(Axis.Col, false)]
    [TestCase(Axis.Row, false)]
    public void Cell_Shift_Restores(Axis axis, bool insert)
    {
        var store = GetGrid();
        var before = Snapshot(store);
        var region = new Region(1, 2, 1, 2);
        var restore = insert ? store.InsertCellsAt(region, axis) : store.RemoveCellsAt(region, axis);
        Snapshot(store).Should().NotBeEquivalentTo(before);

        restore.Shifts.Should().ContainSingle();
        restore.Shifts![0].Band.Should().BeEquivalentTo(region);
        restore.Shifts![0].Amount.Should().Be(insert ? 2 : -2);

        store.Restore(restore);
        Snapshot(store).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
    }

    [Test]
    public void Two_Cell_Shifts_Restore_In_Reverse()
    {
        var store = GetGrid();
        var before = Snapshot(store);
        var r1 = store.InsertCellsAt(new Region(0, 1, 2, 2), Axis.Col);
        var r2 = store.RemoveCellsAt(new Region(1, 3, 3, 3), Axis.Row);
        store.Restore(r2);
        store.Restore(r1);
        Snapshot(store).Should().BeEquivalentTo(before, o => o.WithStrictOrdering());
    }

    [Test]
    public void Whole_Axis_Shifts_Still_Have_No_Band()
    {
        var store = GetGrid();
        store.InsertRowAt(1, 1).Shifts!.Single().Band.Should().BeNull();
        store.RemoveColAt(1, 1).Shifts!.Single().Band.Should().BeNull();
    }
}
