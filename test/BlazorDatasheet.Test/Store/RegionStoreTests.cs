using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using NUnit.Framework;
using FluentAssertions;

namespace BlazorDatasheet.Test.Store;

public class RegionStoreTests
{
    [Test]
    public void Add_And_Retrieve_Region_Data_Correct()
    {
        var store = new RegionDataStore<bool>();
        int r0 = 1;
        int r1 = 2;
        int c0 = 1;
        int c1 = 2;
        store.Add(new Region(r0, r1, c0, c1), true);

        store.GetAllDataRegions().Count().Should().Be(1);

        store.GetDataRegions(r0, c0).First().Data.Should().Be(true);
        store.GetDataRegions(r1, c0).First().Data.Should().Be(true);
        store.GetDataRegions(r0, c1).First().Data.Should().Be(true);
        store.GetDataRegions(r1, c1).First().Data.Should().Be(true);

        store.GetData(r0, c0).First().Should().Be(true);
        store.GetData(r1, c0).First().Should().Be(true);
        store.GetData(r0, c1).First().Should().Be(true);
        store.GetData(r1, c1).First().Should().Be(true);

        store.GetDataRegions(r0 - 1, c0).Should().BeEmpty();
        store.GetDataRegions(r1 + 1, c0).Should().BeEmpty();
        store.GetDataRegions(r0, c1 + 1).Should().BeEmpty();
        store.GetDataRegions(r1, c1 + 1).Should().BeEmpty();
    }

    [Test]
    public void Add_Single_Row_Col_Region_Does_Not_Retrieve_Vals_Around_It()
    {
        var store = new RegionDataStore<bool>();
        store.Add(new Region(1, 1, 1, 1), true);

        store.GetDataRegions(1, 1).First().Data.Should().Be(true);
        store.GetDataRegions(0, 1).Should().BeEmpty();
        store.GetDataRegions(2, 1).Should().BeEmpty();
        store.GetDataRegions(2, 2).Should().BeEmpty();
    }

    [Test]
    public void Remove_Rows_Shifts_And_Removes_And_Contracts_regions()
    {
        /*
               0  1  2  3  4  5
           0 | 5|  |  |  | 3|  |
           1 | 1|  |  |  | 3|  |
           2 |  |  | 2| 2| 3|  |
           3 |  |  | 2| 2|  |  |
           4 |  |  |  |  |  |  |
           5 |  |  |  |  |  | 4|

         */
        var store = new RegionDataStore<int>();
        store.Add(new Region(1, 0), 1); //R1
        store.Add(new Region(2, 3, 2, 3), 2); // R2
        store.Add(new Region(0, 2, 4, 4), 3); // R3
        store.Add(new Region(5, 5), 4); // R4
        store.Add(new Region(0, 0), 5); // R5

        store.RemoveRowAt(1, 3);

        /*
               0  1  2  3  4  5
           0 | 5|  |  |  | 3|  |
           1 |  |  |  |  |  |  |
           2 |  |  |  |  |  | 4|

         */

        store.GetData(0, 0).Should().NotBeEmpty();
        store.GetData(0, 1).Should().BeEmpty();
        store.GetData(0, 4).Should().NotBeEmpty();
        store.GetData(1, 0).Should().BeEmpty();
        store.GetData(1, 4).Should().BeEmpty();
        store.GetData(2, 4).Should().BeEmpty();
        store.GetData(1, 5).Should().BeEmpty();
        store.GetData(2, 5).Should().NotBeEmpty();
        store.GetAllDataRegions().Count().Should().Be(3);
    }

    [Test]
    public void Insert_Rows_Shifts_And_Expands_regions()
    {
        /*
               0  1  2  3  4  5
           0 | 5|  |  |  | 3|  |
           1 | 1|  |  |  | 3|  |
       --> 2 |  |  | 2| 2| 3|  |
           3 |  |  | 2| 2|  |  |
           4 |  |  |  |  |  |  |
           5 |  |  |  |  |  | 4|

         */
        var store = new RegionDataStore<int>(minArea: 0, expandWhenInsertAfter: true);

        store.Add(new Region(1, 0), 1); //R1
        store.Add(new Region(2, 3, 2, 3), 2); // R2
        store.Add(new Region(0, 2, 4, 4), 3); // R3
        store.Add(new Region(5, 5), 4); // R4
        store.Add(new Region(0, 0), 5); // R5

        store.InsertRowColAt(2, 2, Axis.Row);

        /*
               0  1  2  3  4  5
           0 | 5|  |  |  | 3|  |
           1 | 1|  |  |  | 3|  |
           2 | 1|  |  |  | 3|  |
           3 | 1|  |  |  | 3|  |
           4 |  |  | 2| 2| 3|  |
           5 |  |  | 2| 2|  |  |
           6 |  |  |  |  |  |  |
           7 |  |  |  |  |  | 4|

         */

        store.GetAllDataRegions().Count().Should().Be(5);
        store.GetData(0, 0).Should().NotBeEmpty();
        store.GetData(1, 0).Should().NotBeEmpty();
        store.GetData(3, 0).Should().NotBeEmpty();
        store.GetData(0, 4).Should().NotBeEmpty();
        store.GetData(4, 4).Should().NotBeEmpty();
        store.GetData(2, 2).Should().BeEmpty();
        store.GetData(6, 5).Should().BeEmpty();
        store.GetData(7, 5).Should().NotBeEmpty();
    }

    [Test]
    public void Insert_Row_At_Same_Row_As_Data_Shifts_Down()
    {
        var store = new RegionDataStore<int>();
        store.Add(new Region(0, 0), -1);
        store.InsertRowColAt(0, 1, Axis.Row);
        store.GetData(0, 0).Should().BeEmpty();
        store.GetData(1, 0).Should().ContainSingle(x => x == -1);
    }

    [Test]
    public void Delete_Row_Before_Data_Restores_Correctly()
    {
        var store = new RegionDataStore<bool>();
        var restore1 = store.Add(new Region(1, 1), true);
        var restoreData = store.RemoveColAt(0, 1);
        store.Restore(restoreData);
        store.GetData(1, 1).Should().BeEquivalentTo([true]);
        store.Restore(restore1);
        store.GetAllDataRegions().Should().BeEmpty();
    }

    [Test]
    public void Insert_Col_At_Same_Col_As_Data_Shifts_Right()
    {
        var store = new RegionDataStore<int>();
        store.Add(new Region(0, 0), -1);
        var restore = store.InsertRowColAt(0, 1, Axis.Col);
        store.GetData(0, 0).Should().BeEmpty();
        store.GetData(0, 1).Should().ContainSingle(x => x == -1);
        store.Restore(restore);
        store.GetData(0, 0).Should().BeEquivalentTo([-1]);
    }

    [Test]
    public void Delete_First_Col_Of_region_Restores()
    {
        var store = new RegionDataStore<int>();
        var r = new Region(1, 5, 1, 5);
        store.Add(r, 1);
        var restoreData = store.RemoveColAt(1, 1);
        store.GetAllDataRegions()
            .Select(x => x.Region)
            .Should().BeEquivalentTo([new Region(1, 5, 1, 4)]);
        store.Restore(restoreData);
        store.GetAllDataRegions()
            .Select(x => x.Region)
            .Should().BeEquivalentTo([r]);
    }

    [Test]
    public void Delete_First_Cols_From_Behind_region_Restores()
    {
        var store = new RegionDataStore<int>();
        var r = new Region(1, 5, 1, 5);
        store.Add(r, 1);
        var restoreData = store.RemoveColAt(0, 1);
        store.GetAllDataRegions()
            .Select(x => x.Region)
            .Should().BeEquivalentTo([new Region(1, 5, 0, 3)]);
        store.Restore(restoreData);
        store.GetAllDataRegions()
            .Select(x => x.Region)
            .Should().BeEquivalentTo([r]);
    }

    [Test]
    public void Copy_Region_With_Partial_Data_Copies_And_Restores()
    {
        var store = new RegionDataStore<int>();
        var region = new Region(0, 5, 0, 5);
        store.Add(region, -1);
        var restoreData = store.Copy(new Region(2, 5, 2, 5), new CellPosition(0, 6));
        store.GetData(0, 0).Should().ContainSingle(x => x == -1);
        store.GetData(5, 5).Should().ContainSingle(x => x == -1);
        store.GetData(0, 6).Should().ContainSingle(x => x == -1);
        store.GetData(3, 6).Should().ContainSingle(x => x == -1);
        store.GetData(3, 9).Should().ContainSingle(x => x == -1);
        store.GetData(4, 10).Should().BeEmpty();

        store.Restore(restoreData);
        store.GetAllDataRegions().Should().ContainSingle(x => x.Region == region && x.Data == -1);
    }

    [Test]
    public void Clear_All_Data_Clears_All_Data()
    {
        var store = new RegionDataStore<int>();
        store.Add(new Region(0, 5, 0, 5), -1);
        store.Add(new Region(2, 6, 2, 6), -1);
        store.Clear(new Region(0, 5, 0, 5));
        store.GetDataRegions(new Region(0, 5, 0, 5)).Should().BeEmpty();
    }

    [Test]
    public void Get_Sub_Store_Gets_Sub_Storage_Works_When_Not_Resetting_Indices()
    {
        var store = new RegionDataStore<int>();
        store.Add(new ColumnRegion(1), 1);
        store.Add(new ColumnRegion(2), 2);

        var subStore = store.GetSubStore(new RowRegion(3, 4), false);
        subStore.GetData(3, 1).Should().BeEquivalentTo(new int[] { 1 });
        subStore.GetData(4, 2).Should().BeEquivalentTo(new int[] { 2 });
    }

    [Test]
    public void Get_Sub_Store_Gets_Sub_Storage_Works_When_Resetting_Indices()
    {
        var store = new RegionDataStore<int>();
        store.Add(new ColumnRegion(1), 1);
        store.Add(new ColumnRegion(2), 2);

        var subStore = store.GetSubStore(new RowRegion(3, 4), newStoreResetsOffsets: true);
        subStore.GetData(0, 1).Should().BeEquivalentTo(new int[] { 1 });
        subStore.GetData(1, 2).Should().BeEquivalentTo(new int[] { 2 });
    }

    [Test]
    public void Removing_Across_Column_Region_Doesnt_Overflow()
    {
        var store = new ConsolidatedDataStore<int>();
        store.Add(new ColumnRegion(10, 20), 1);

        store.RemoveRowAt(10, 10);
    }
}