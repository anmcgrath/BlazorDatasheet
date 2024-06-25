using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// A wrapper around an RTree enabling storing data in regions.
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class RegionDataStore<T> : IStore<T, RegionRestoreData<T>> where T : IEquatable<T>
{
    protected readonly int MinArea;
    protected readonly bool ExpandWhenInsertAfter;
    protected readonly RTree<DataRegion<T>> Tree;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="minArea">The minimum area that regions can have before they will be removed after operations.
    /// If a region's area ends up less than or equal to this, the region will be removed.</param>
    /// <param name="expandWhenInsertAfter">When set to true, when a row or column is inserted just below/right of a region, the region is expanded</param>
    public RegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
    {
        MinArea = minArea;
        ExpandWhenInsertAfter = expandWhenInsertAfter;
        Tree = new RTree<DataRegion<T>>();
    }

    public bool Contains(int row, int col)
    {
        return GetDataRegions(row, col).Any();
    }

    /// <summary>
    /// Returns all data regions in the store.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DataRegion<T>> GetAllDataRegions()
    {
        return Tree.Search();
    }

    public IEnumerable<DataRegion<T>> GetDataRegions(int row, int col)
    {
        return GetDataRegions(new Region(row, col));
    }

    /// <summary>
    /// Returns all data regions overlapping the regions
    /// </summary>
    public IEnumerable<DataRegion<T>> GetDataRegions(IEnumerable<IRegion> regions)
    {
        return regions.SelectMany(GetDataRegions);
    }

    /// <summary>
    /// Returns all data regions overlapping the region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<DataRegion<T>> GetDataRegions(IRegion region)
    {
        var env = region.ToEnvelope();
        return Tree.Search(env);
    }

    /// <summary>
    /// Gets Data regions where the data is the same as the <paramref name="data"/> given
    /// and the region is exactly the same
    /// </summary>
    /// <param name="region"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public IEnumerable<DataRegion<T>> GetDataRegions(IRegion region, T data)
    {
        return GetDataRegions(region)
            .Where(x => x.Data.Equals(data) && x.Region.Equals(region));
    }

    public IEnumerable<T> GetData(int row, int col)
    {
        return GetDataRegions(row, col).Select(x => x.Data);
    }

    public IEnumerable<T> GetData(IRegion region)
    {
        return GetDataRegions(region).Select(x => x.Data);
    }

    /// <summary>
    /// Returns the data regions that are contained inside the region.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    internal IEnumerable<DataRegion<T>> GetContainedRegions(IRegion region)
    {
        return Tree.Search(region.ToEnvelope())
            .Where(x => region.Contains(x.Region));
    }

    /// <summary>
    /// Adds a region to the store, and assigns it data.
    /// Overlapping regions are not removed.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="data"></param>
    public RegionRestoreData<T> Add(IRegion region, T data)
    {
        return Add(new DataRegion<T>(data, region));
    }

    /// <summary>
    /// Inserts rows or columns (depending on <paramref name="axis"/>) and shifts/expand regions appropriately.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
    {
        var expand = ExpandWhenInsertAfter;

        // As an example for inserting rows, there are three things that can happen
        // 1. Any regions that intersect the index should be expanded by nRows
        // 2. If _expandWhenInsertedAfter = true, then when the bottom of any region is just above index, expand it too.
        // 3. Any regions below the index should be shifted down
        var i0 = expand ? index - 1 : index;
        IRegion region = axis == Axis.Col ? new ColumnRegion(i0, index) : new RowRegion(i0, index);
        var overlapping = GetDataRegions(region);
        var dataRegionsToAdd = new List<DataRegion<T>>();
        var regionsAdded = new List<DataRegion<T>>();
        var regionsRemoved = new List<DataRegion<T>>();

        foreach (var overlap in overlapping)
        {
            if (overlap.Region.GetLeadingEdgeOffset(axis) == index)
                continue; // we shift in this case, and don't expand
            var i1 = overlap.Region.GetTrailingEdgeOffset(axis);
            if (!expand && index > i1)
                continue;

            Tree.Delete(overlap);
            regionsRemoved.Add(overlap);
            var expanded = new DataRegion<T>(overlap.Data, overlap.Region.Clone());
            expanded.Region.Expand(axis == Axis.Row ? Edge.Bottom : Edge.Right, count);
            expanded.UpdateEnvelope();
            regionsAdded.Add(expanded);
            dataRegionsToAdd.Add(expanded);
        }

        // index - 1 because the top of the region has to be above the region to shift it down
        var below = this.GetAfter(index - 1, axis);
        foreach (var r in below)
        {
            Tree.Delete(r);
            var dRow = axis == Axis.Row ? count : 0;
            var dCol = axis == Axis.Col ? count : 0;
            r.Shift(dRow, dCol);
            dataRegionsToAdd.Add(r);
        }

        Tree.BulkLoad(dataRegionsToAdd);
        return new RegionRestoreData<T>()
        {
            RegionsAdded = regionsAdded,
            RegionsRemoved = regionsRemoved,
            Shifts = [new(axis, index, count)],
        };
    }

    /// <summary>
    /// Updates the region positions by handling row/columns deletes. Regions are shifted/contracted appropriately.
    /// Returns any data that is removed during this operation.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public RegionRestoreData<T> RemoveRowColAt(int index, int count, Axis axis)
    {
        if (axis == Axis.Col)
            return RemoveColAt(index, index + count - 1);
        else
            return RemoveRowAt(index, index + count - 1);
    }

    /// <summary>
    /// Updates the region positions by handling row deletes. Regions are shifted/contracted appropriately.
    /// Returns any data that is removed during this operation.
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <returns>Any data that is removed during this operation</returns>
    public RegionRestoreData<T> RemoveRowAt(int rowStart, int rowEnd) =>
        RemoveRowsOrColumsAndShift(rowStart, rowEnd, Axis.Row);

    /// <summary>
    /// Updates the region positions by handling column deletes. Regions are shifted/contracted appropriately.
    /// Returns any data that is removed during this operation.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="count"></param>
    /// <returns>Any data that is removed during this operation</returns>
    public RegionRestoreData<T> RemoveColAt(int colStart, int count) =>
        RemoveRowsOrColumsAndShift(colStart, count, Axis.Col);

    /// <summary>
    /// Removes a number of rows or columns and shifts/contracts regions appropriately.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private RegionRestoreData<T> RemoveRowsOrColumsAndShift(int start, int end, Axis axis)
    {
        IRegion region = axis == Axis.Col ? new ColumnRegion(start, end) : new RowRegion(start, end);
        // keep track of any removed data regions for the restore data.
        var removed = new List<DataRegion<T>>();

        var overlapping = GetDataRegions(region);
        var newDataToAdd = new List<DataRegion<T>>();
        var dataAdded = new List<DataRegion<T>>();

        foreach (var overlap in overlapping)
        {
            // If the overlapping region is fully contained within the region, remove it.
            if (region.Contains(overlap.Region))
            {
                Tree.Delete(overlap);
                removed.Add(overlap);
                continue;
            }

            // if the region is partially overlapping (it must be because we know it is overlapping and it doesn't fully
            // overlap) then shift the overlap left and contract the left edge

            var intersection = overlap.Region.GetIntersection(region)!;
            // contraction amount in row direction
            var cRow = axis == Axis.Row ? intersection.Height : 0;
            // contraction amount in col direction
            var cCol = axis == Axis.Col ? intersection.Width : 0;
            var cEdge = axis == Axis.Col ? Edge.Right : Edge.Bottom;
            var shift = start - overlap.Region.GetLeadingEdgeOffset(axis);
            if (shift > 0)
                shift = 0;
            var sRow = axis == Axis.Row ? shift : 0;
            var sCol = axis == Axis.Col ? shift : 0;

            Tree.Delete(overlap);
            removed.Add(overlap);

            var newRegion = new DataRegion<T>(overlap.Data, overlap.Region.Clone());
            newRegion.Region.Contract(cEdge, Math.Max(cRow, cCol));
            newRegion.Region.Shift(sRow, sCol);

            // if the region is less than the minimum area, don't add it back
            if (newRegion.Region.Area <= MinArea)
                continue;

            newRegion.UpdateEnvelope();
            dataAdded.Add(newRegion);
            newDataToAdd.Add(newRegion);
        }

        // shift anything right or below the removed region right/down
        var dataToShift = GetAfter(end, axis);
        foreach (var dataRegion in dataToShift)
        {
            Tree.Delete(dataRegion);
            var nRows = axis == Axis.Row ? region.Height : 0;
            var nCols = axis == Axis.Col ? region.Width : 0;
            dataRegion.Shift(-nRows, -nCols);
            newDataToAdd.Add(dataRegion);
        }

        Tree.BulkLoad(newDataToAdd);

        return new RegionRestoreData<T>()
        {
            RegionsRemoved = removed,
            RegionsAdded = dataAdded,
            Shifts = [new(axis, start - 1, -(end - start + 1))],
        };
    }

    /// <summary>
    /// Gets data to the right/below the given row or column.
    /// </summary>
    /// <param name="rowOrCol"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private IEnumerable<DataRegion<T>> GetAfter(int rowOrCol, Axis axis)
    {
        switch (axis)
        {
            case Axis.Row:
                return this.GetDataRegions(new RowRegion(rowOrCol + 1, int.MaxValue))
                    .Where(x => x.Region.Top >= rowOrCol + 1);
            case Axis.Col:
                return this.GetDataRegions(new ColumnRegion(rowOrCol + 1, int.MaxValue))
                    .Where(x => x.Region.Left >= rowOrCol + 1);
        }

        return Enumerable.Empty<DataRegion<T>>();
    }

    /// <summary>
    /// Clears the regions from overlapping data where the data is the same.
    /// Returns the regions that were removed/added in the process.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public virtual RegionRestoreData<T> Clear(IRegion region, T data)
    {
        var overlapping = GetDataRegions(region)
            .Where(x => x.Data.Equals(data));
        return Clear(region, overlapping);
    }

    /// <summary>
    /// Clears all data from a region.
    /// </summary>
    /// <param name="region"></param>
    public virtual RegionRestoreData<T> Clear(IRegion region)
    {
        return Clear(region, GetDataRegions(region));
    }

    public RegionRestoreData<T> InsertRowAt(int row, int count) => InsertRowColAt(row, count, Axis.Row);

    public RegionRestoreData<T> InsertColAt(int col, int count) => InsertRowColAt(col, count, Axis.Col);

    /// <summary>
    /// Clears all data
    /// </summary>
    /// <returns></returns>
    public virtual RegionRestoreData<T> Clear()
    {
        var allData = Tree.Search();
        var restoreData = new RegionRestoreData<T>()
        {
            RegionsRemoved = allData.ToList()
        };
        Tree.Clear();
        return restoreData;
    }

    /// <summary>
    /// Returns a sub-store containing only the data in the region specified.
    /// If the <paramref name="newStoreResetsOffsets"/> is true, the new store will have the top-left corner at 0,0.
    /// </summary>
    /// <param name="region">The region to extract data from</param>
    /// <param name="newStoreResetsOffsets">If true, the new store will have the top-left corner at 0,0</param>
    /// <returns></returns>
    public RegionDataStore<T> GetSubStore(IRegion region, bool newStoreResetsOffsets = true)
    {
        var store = GetEmptyClone();
        var data =
            GetDataRegions(region)
                .Select(x =>
                {
                    var newRegion = x.Region.GetIntersection(region)!;
                    if (newStoreResetsOffsets)
                    {
                        newRegion.Shift(-region.Top, -region.Left);
                    }

                    return new DataRegion<T>(x.Data, newRegion);
                });

        store.AddRange(data);
        return store;
    }

    protected virtual RegionDataStore<T> GetEmptyClone()
    {
        return new RegionDataStore<T>(MinArea, ExpandWhenInsertAfter);
    }

    private RegionRestoreData<T> Clear(IRegion region, IEnumerable<DataRegion<T>> dataRegions)
    {
        var dataRegionsToRemove = new List<DataRegion<T>>();
        var dataRegionsToAdd = new List<DataRegion<T>>();

        foreach (var overlap in dataRegions)
        {
            dataRegionsToRemove.Add(overlap);
            if (region.Contains(overlap.Region))
                continue;

            var breakRegions = overlap.Region.Break(region);
            dataRegionsToAdd.AddRange(breakRegions.Select(x => new DataRegion<T>(overlap.Data, x)));
        }

        foreach (var regionToRemove in dataRegionsToRemove)
            Tree.Delete(regionToRemove);

        Tree.BulkLoad(dataRegionsToAdd);

        return new RegionRestoreData<T>()
        {
            RegionsRemoved = dataRegionsToRemove,
            RegionsAdded = dataRegionsToAdd
        };
    }

    public RegionRestoreData<T> Copy(IRegion fromRegion, CellPosition toPosition)
    {
        var toRegion = new Region(
            toPosition.row, toPosition.row + fromRegion.Height - 1,
            toPosition.col, toPosition.col + fromRegion.Width - 1);

        var dr = toRegion.Top - fromRegion.Top;
        var dc = toRegion.Left - fromRegion.Left;

        // collect the data that we need to add into our cleared region first because
        // it may be need to be cleared.
        var dataToCopy = Grab(fromRegion);
        var restoreData = this.Clear(toRegion);
        foreach (var d in dataToCopy)
        {
            d.Region.Shift(dr, dc);
            d.UpdateEnvelope();
        }

        Tree.BulkLoad(dataToCopy);
        restoreData.RegionsAdded.AddRange(dataToCopy);

        return restoreData;
    }

    /// <summary>
    /// Gets *new* data regions inside the <paramref name="region"/> given, limited to those regions
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    private List<DataRegion<T>> Grab(IRegion region)
    {
        var grabbedData = new List<DataRegion<T>>();
        var data = this.GetDataRegions(region);
        foreach (var r in data)
        {
            grabbedData.Add(new DataRegion<T>(r.Data, region.GetIntersection(r.Region)!));
        }

        return grabbedData;
    }

    /// <summary>
    /// Adds a data region to the store.
    /// Overlapping regions are not removed.
    /// </summary>
    /// <param name="dataRegion"></param>
    protected virtual RegionRestoreData<T> Add(DataRegion<T> dataRegion)
    {
        Tree.Insert(dataRegion);
        return new RegionRestoreData<T>()
        {
            RegionsAdded = new List<DataRegion<T>>() { dataRegion }
        };
    }

    public virtual RegionRestoreData<T> Set(int row, int col, T value)
    {
        var region = new Region(row, col);
        var restoreData = Clear(region);
        restoreData.Merge(this.Add(new DataRegion<T>(value, region)));
        return restoreData;
    }

    public RegionRestoreData<T> Delete(DataRegion<T> dataRegion)
    {
        Tree.Delete(dataRegion);
        return new RegionRestoreData<T>()
        {
            RegionsRemoved = [dataRegion]
        };
    }

    public RegionRestoreData<T> Delete(IEnumerable<DataRegion<T>> dataRegions)
    {
        return dataRegions.Select(Delete)
            .Aggregate((x, y) => x.Merge(y));
    }

    public bool Any() => Tree.Count > 0;

    public bool Any(int row, int col)
    {
        return GetDataRegions(row, col).Any();
    }

    public bool Any(IRegion region)
    {
        return GetDataRegions(region).Any();
    }

    protected void AddRange(IEnumerable<DataRegion<T>> dataRegions)
    {
        Tree.BulkLoad(dataRegions);
    }

    public virtual void Restore(RegionRestoreData<T> restoreData)
    {
        foreach (var shift in restoreData.Shifts)
        {
            var regionsToShift = GetAfter(shift.Index, shift.Axis);
            foreach (var region in regionsToShift)
            {
                Tree.Delete(region);
                var dCol = shift.Axis == Axis.Col ? -shift.Amount : 0;
                var dRow = shift.Axis == Axis.Row ? -shift.Amount : 0;
                region.Region.Shift(dRow, dCol);
                region.UpdateEnvelope();
                Tree.Insert(region);
            }
        }

        foreach (var added in restoreData.RegionsAdded)
        {
            Tree.Delete(added);
        }

        Tree.BulkLoad(restoreData.RegionsRemoved);
    }
}