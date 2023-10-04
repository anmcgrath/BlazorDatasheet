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

        store.GetRegionsOverlapping(r0, c0).First().Data.Should().Be(true);
        store.GetRegionsOverlapping(r1, c0).First().Data.Should().Be(true);
        store.GetRegionsOverlapping(r0, c1).First().Data.Should().Be(true);
        store.GetRegionsOverlapping(r1, c1).First().Data.Should().Be(true);

        store.GetDataOverlapping(r0, c0).First().Should().Be(true);
        store.GetDataOverlapping(r1, c0).First().Should().Be(true);
        store.GetDataOverlapping(r0, c1).First().Should().Be(true);
        store.GetDataOverlapping(r1, c1).First().Should().Be(true);

        store.GetRegionsOverlapping(r0 - 1, c0).Should().BeEmpty();
        store.GetRegionsOverlapping(r1 + 1, c0).Should().BeEmpty();
        store.GetRegionsOverlapping(r0, c1 + 1).Should().BeEmpty();
        store.GetRegionsOverlapping(r1, c1 + 1).Should().BeEmpty();
    }

    [Test]
    public void Add_Single_Row_Col_Region_Does_Not_Retrieve_Vals_Around_It()
    {
        var store = new RegionDataStore<bool>();
        store.Add(new Region(1, 1, 1, 1), true);

        store.GetRegionsOverlapping(1, 1).First().Data.Should().Be(true);
        store.GetRegionsOverlapping(0, 1).Should().BeEmpty();
        store.GetRegionsOverlapping(2, 1).Should().BeEmpty();
        store.GetRegionsOverlapping(2, 2).Should().BeEmpty();
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

        store.RemoveRows(1, 3);

        /*
               0  1  2  3  4  5
           0 | 5|  |  |  | 3|  |
           1 |  |  |  |  |  |  |
           2 |  |  |  |  |  | 4|

         */

        store.GetDataOverlapping(0, 0).Should().NotBeEmpty();
        store.GetDataOverlapping(0, 1).Should().BeEmpty();
        store.GetDataOverlapping(0, 4).Should().NotBeEmpty();
        store.GetDataOverlapping(1, 0).Should().BeEmpty();
        store.GetDataOverlapping(1, 4).Should().BeEmpty();
        store.GetDataOverlapping(2, 4).Should().BeEmpty();
        store.GetDataOverlapping(1, 5).Should().BeEmpty();
        store.GetDataOverlapping(2, 5).Should().NotBeEmpty();
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

        store.InsertRows(2, 2);

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
        store.GetDataOverlapping(0, 0).Should().NotBeEmpty();
        store.GetDataOverlapping(1, 0).Should().NotBeEmpty();
        store.GetDataOverlapping(3, 0).Should().NotBeEmpty();
        store.GetDataOverlapping(0, 4).Should().NotBeEmpty();
        store.GetDataOverlapping(4, 4).Should().NotBeEmpty();
        store.GetDataOverlapping(2, 2).Should().BeEmpty();
        store.GetDataOverlapping(6, 5).Should().BeEmpty();
        store.GetDataOverlapping(7, 5).Should().NotBeEmpty();
    }
}