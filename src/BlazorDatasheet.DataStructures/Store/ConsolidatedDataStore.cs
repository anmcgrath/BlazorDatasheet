using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// A region store where regions will not overlap if added regions contain the same data.
/// If a newly added region overlaps with any regions containing the same data, regions will be added/removed
/// to consolidate these regions into a smaller number of non-overlapping regions.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ConsolidatedDataStore<T> : RegionDataStore<T> where T : IEquatable<T>
{
    /// <summary>
    /// Keeps track of the regions that each data apply to.
    /// </summary>
    private readonly Dictionary<T, List<IRegion>> _dataMaps;

    public ConsolidatedDataStore() : base()
    {
        _dataMaps = new Dictionary<T, List<IRegion>>();
    }
    
    public ConsolidatedDataStore(int minArea, bool expandWhenInsertAfter) : base(minArea, expandWhenInsertAfter)
    {
        _dataMaps = new Dictionary<T, List<IRegion>>();
    }

    protected override RegionRestoreData<T> Add(DataRegion<T> dataRegion)
    {
        var (regionsToRemove, regionsToAdd) = Consolidate(dataRegion);
        foreach (var removal in regionsToRemove)
            Tree.Delete(removal);
        Tree.BulkLoad(regionsToAdd);

        _dataMaps.TryAdd(dataRegion.Data, regionsToAdd.Select(x => x.Region).ToList());

        foreach (var removal in regionsToRemove)
        {
            // since we are grabbing the regions from the tree,
            // the region should refer to the same object as the data map stores.
            _dataMaps[dataRegion.Data].Remove(removal.Region);
        }

        return new RegionRestoreData<T>()
        {
            RegionsAdded = regionsToAdd.ToList(),
            RegionsRemoved = regionsToRemove.ToList()
        };
    }

    public override RegionRestoreData<T> Clear(IRegion region, T data)
    {
        if (!_dataMaps.ContainsKey(data))
            return new RegionRestoreData<T>();

        var restoreData = base.Clear(region, data);
        foreach (var removed in restoreData.RegionsRemoved)
            _dataMaps[data].Remove(removed.Region);
        foreach (var added in restoreData.RegionsAdded)
            _dataMaps[data].Add(added.Region);

        return restoreData;
    }

    protected override RegionDataStore<T> GetEmptyClone()
    {
        return new ConsolidatedDataStore<T>();
    }

    /// <summary>
    /// Returns the regions associated with the given data.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public IEnumerable<IRegion> GetRegions(T data)
    {
        //TODO use data maps... or delete them
        // since we won't be calling this very often it doesn't have to be efficient?
        return Tree.Search().Where(x => x.Data.Equals(data)).Select(x => x.Region);
    }

    /// <summary>
    /// Returns the list of regions that should be removed/added when so that an added data region
    /// results in non-overlapping regions.
    /// </summary>
    /// <param name="dataRegion"></param>
    /// <returns></returns>
    private (List<DataRegion<T>> regionsToRemove, List<DataRegion<T>> regionsToAdd) Consolidate(
        DataRegion<T> dataRegion)
    {
        // the assumption (which is true for this class) is that regions are always consolidated
        // when new ones are added, so we assume that all existing regions are non-overlapping.
        // There is most definitely a more efficient way of doing this, because we will end up with more regions
        // than we need to.
        var overlappingData = GetDataRegions(dataRegion.Region)
            .Where(x => x.Data.Equals(dataRegion.Data))
            .ToList();

        var regionsToRemove = new List<DataRegion<T>>();
        var regionsToAdd = new List<DataRegion<T>>();

        // if any of the regions contain the new one, we don't need to add it
        var fullyContained = overlappingData.Any(x => x.Region.Contains(dataRegion.Region));
        if (fullyContained)
            return (regionsToRemove, regionsToAdd);

        regionsToAdd.Add(dataRegion);

        // We are overlapping but not fully contained, so all these regions will be removed - either because of a break
        // or because we contain the region.
        regionsToRemove.AddRange(overlappingData);

        for (int i = 0; i < overlappingData.Count; i++)
        {
            // We just have to deal with partial intersections
            if (dataRegion.Region.Contains(overlappingData[i].Region))
                continue;
            var breaks = overlappingData[i].Region.Break(dataRegion.Region);
            regionsToAdd.AddRange(breaks.Select(x => new DataRegion<T>(dataRegion.Data, x)));
        }

        return (regionsToRemove, regionsToAdd);
    }

    /// <summary>
    /// Returns the data at row col, if any.
    /// If no data exists, default(T?) is returned.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public T? Get(int row, int col)
    {
        // Since this is a consolidated data store, we must have only 
        // one overlapping bit of data.
        return this.GetData(row, col).FirstOrDefault();
    }
}