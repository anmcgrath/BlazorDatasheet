using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class ConsolidatedRegionStoreTests
{
    [Test]
    public void Add_Region_Inside_Results_In_One_Region()
    {
        var store = new ConsolidatedDataStore<int>();
        var r0 = new Region(0, 5, 0, 5);
        store.Add(r0, 1);
        store.Add(new Region(0, 4, 0, 4), 1);
        store.GetRegionsForData(1).Count().Should().Be(1);
        store.GetRegionsForData(1).First().Should().Be(r0);
    }

    [Test]
    public void Add_Region_With_Different_Data_Results_In_Two_Regions()
    {
        var store = new ConsolidatedDataStore<int>();
        var r0 = new Region(0, 5, 0, 5);
        var r1 = new Region(0, 4, 0, 4);
        store.Add(r0, 0);
        store.Add(r1, 1);
        store.GetRegionsForData(0).Should().ContainSingle(x => x == r0);
        store.GetRegionsForData(1).Should().ContainSingle(x => x == r1);

        store.GetDataOverlapping(0, 0).Should().Equal(new[] { 0, 1 });
    }

    [Test]
    public void Add_Region_Intersecting_Results_In_No_Overlap()
    {
        var store = new ConsolidatedDataStore<int>();
        store.Add(new Region(0, 5, 0, 5), 0);
        store.Add(new Region(4, 6, 0, 5), 0);
        store.Add(new Region(1, 1, 4, 5), 0);
        var regions = store.GetAllDataRegions();
        foreach (var r in regions)
        {
            foreach (var other in regions)
            {
                if (r != other)
                    r.Region.GetIntersection(other.Region).Should().Be(null);
            }
        }
    }

    [Test]
    public void Cut_Region_Entirely_Results_In_No_Regions()
    {
        var store = new ConsolidatedDataStore<int>();
        store.Add(new Region(0, 5, 0, 5), 1);
        store.Cut(new Region(0, 5, 0, 5), 1);
        store.GetRegionsForData(1).Should().BeEmpty();
        store.GetDataOverlapping(0, 0).Should().BeEmpty();
    }

    [Test]
    public void Cut_region_Entirely_From_Other_Data_results_In_No_CHange()
    {
        var r0 = new Region(0, 5, 0, 5);
        var store = new ConsolidatedDataStore<int>();
        store.Add(r0, 1);
        store.Cut(r0, 2); // note data is different
        var regions = store.GetRegionsForData(1).ToList();
        regions.Should().ContainSingle(x => x == r0);
        store.GetDataOverlapping(0, 0).Should().ContainSingle(x => x == 1);
    }

    [Test]
    public void Cut_Region_Inside_Region_Results_Results_In_Correct_Cut()
    {
        var r0 = new Region(0, 5, 0, 5);
        var store = new ConsolidatedDataStore<int>();
        store.Add(r0, 1);
        store.Cut(new Region(2, 3, 2, 3), 1);
        store.GetDataOverlapping(2, 2).Should().BeEmpty();
        store.GetDataOverlapping(3, 3).Should().BeEmpty();
        store.GetDataOverlapping(0, 0).Should().ContainSingle(x => x == 1);
        store.GetDataOverlapping(4, 4).Should().ContainSingle(x => x == 1);
    }

    [Test]
    public void Cut_Region_Results_In_Correct_Data_Region_References()
    {
        var store = new ConsolidatedDataStore<int>();
        var r0 = new Region(0, 5, 0, 5);
        var cutRegion = new Region(2, 3, 2, 3);
        store.Add(r0, 1);
        store.Cut(cutRegion, 1);
        store.GetRegionsForData(1).Sum(x => x.Area).Should().Be(r0.Area - cutRegion.Area);
    }

    [Test]
    public void Insert_Row_Updates_Store_Data_Region_Map()
    {
        var store = new ConsolidatedDataStore<int>();
        store.Add(new Region(1, 1, 1, 1), 0);
        store.InsertRows(0, 1);
        store.GetDataOverlapping(1, 1).Should().BeEmpty();
        store.GetDataOverlapping(2, 1).Should().NotBeEmpty();
        store.GetRegionsForData(0).First().Top.Should().Be(2);
    }
}