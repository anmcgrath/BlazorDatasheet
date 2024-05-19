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
        var overlaps = this.GetDataRegions(dataRegion.Region);
        // we need to remove any parts that are overlapping, 
        // and add them back in with the data merged

        var toRemove = new List<DataRegion<T>>();
        var toAdd = new List<DataRegion<T>>();
        // we keep track of any merges so that we remove these from the new data
        // before adding the new data in. We do this at the end because the new data
        // may intersect multiple regions.
        var mergesMade = new List<IRegion>();

        // basic premise is:
        // any intersections are added back in as merged data
        // existing data is removed and broken with intersection. those pieces outside of merge are added back in.
        // new data is removed, then broken with all the merges made and then the broken bits are added back in.

        foreach (var overlap in overlaps)
        {
            if (overlap.Region.Contains(dataRegion.Region) &&
                overlap.Data.Equals(dataRegion.Data))
            {
                // nothing new added, since the new data is the same and inside existing
                continue;
            }

            if (dataRegion.Region.Contains(overlap.Region) &&
                dataRegion.Data.Equals(overlap.Data))
            {
                // we can remove the existing data entirely since it is contained and is the same
                toRemove.Add(overlap);
                continue;
            }

            // intersection is never null here because we know it's overlapping.
            var intersection = overlap.Region.GetIntersection(dataRegion.Region)!;
            var mergedData = GetNewMergedData(overlap.Data, dataRegion.Data);
            toAdd.Add(new DataRegion<T>(mergedData, intersection));
            // store the OLD unmerged data at the intersection
            mergesMade.Add(intersection);

            toRemove.Add(overlap);
            var breakExisting = overlap.Region.Break(intersection);
            toAdd.AddRange(breakExisting.Select(x => new DataRegion<T>(overlap.Data, x)));
        }

        // now we need to do the last step which is to break the new data with all the merges made, and then add them back in
        // with the new data
        var breakNewData = dataRegion.Region.Break(mergesMade);
        toAdd.AddRange(breakNewData.Select(x => new DataRegion<T>(dataRegion.Data, x)));

        foreach (var r in toRemove)
            Tree.Delete(r);

        Tree.BulkLoad(toAdd);

        return new RegionRestoreData<T>()
        {
            RegionsAdded = toAdd,
            RegionsRemoved = toRemove
        };
    }

    /// <summary>
    /// Merges newData into copy of new data and returns it.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="newData"></param>
    /// <returns></returns>
    private T GetNewMergedData(T data, T newData)
    {
        var clone = data.Clone();
        clone.Merge(newData);
        return clone;
    }
}