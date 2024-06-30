using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// A storage of regions, where if the region is added, regions are merged into any existing.
/// </summary>
/// <typeparam name="T"></typeparam>
public class MergeRegionDataStore<T> : RegionDataStore<T> where T : IMergeable<T>, IEquatable<T>
{
    public MergeRegionDataStore(int minArea = 0, bool expandOnInsert = true) : base(minArea, expandOnInsert)
    {
    }

    protected override RegionDataStore<T> GetEmptyClone()
    {
        return new MergeRegionDataStore<T>(MinArea, ExpandWhenInsertAfter);
    }

    protected override RegionRestoreData<T> Add(DataRegion<T> dataRegion)
    {
        // we have the valid assumption that only one region will exist at each position
        // because we are always merging regions when adding.
        var overlaps = this.GetDataRegions(dataRegion.Region).ToList();
        // we need to remove any parts that are overlapping, 
        // and add them back in with the data merged

        var toRemove = new List<DataRegion<T>>();
        var toAdd = new List<DataRegion<T>>();
        var mergesMade = new List<IRegion>();

        // basic premise is:
        // any intersections are added back in as merged data
        // existing data is removed and broken with intersection. those pieces outside of merge are added back in.

        foreach (var overlap in overlaps)
        {
            // We will always remove the overlap and replace it with new data
            toRemove.Add(overlap);

            // Intersection is never null here because we know it's overlapping.
            // Merge the intersecting part
            var intersection = overlap.Region.GetIntersection(dataRegion.Region)!;
            var mergedDataRegion = new DataRegion<T>(overlap.Data.Clone(), intersection);
            mergedDataRegion.Data.Merge(dataRegion.Data);
            toAdd.Add(mergedDataRegion);
            // store the merged regions so we can add in around them after
            mergesMade.Add(intersection);

            // add in the parts that are outside the intersection
            // but inside the overlap
            var outside = overlap.Region.Break(intersection);
            toAdd.AddRange(outside.Select(x => new DataRegion<T>(overlap.Data, x)));
        }

        // now add the new data that hasn't been merged
        var newData = dataRegion.Region.Break(mergesMade);
        toAdd.AddRange(newData.Select(x => new DataRegion<T>(dataRegion.Data, x)));

        foreach (var r in toRemove)
            Tree.Delete(r);

        Tree.BulkLoad(toAdd);

        return new RegionRestoreData<T>()
        {
            RegionsAdded = toAdd,
            RegionsRemoved = toRemove
        };
    }
}