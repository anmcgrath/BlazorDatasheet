using System.Collections;
using System.Data.SqlTypes;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// A wrapper around an RTree enabling storing data in regions.
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class RegionDataStore<T> where T : IEquatable<T>
{
    private readonly int _minArea;
    private readonly bool _expandWhenInsertAfter;
    protected RTree<DataRegion<T>> _tree;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="minArea">The minimum area that regions can have before they will be removed after operations.
    /// If a region's area ends up less than or equal to this, the region will be removed.</param>
    /// <param name="expandWhenInsertAfter">When set to true, when a row or column is inserted just below/right of a region, the region is expanded</param>
    public RegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
    {
        _minArea = minArea;
        _expandWhenInsertAfter = expandWhenInsertAfter;
        _tree = new RTree<DataRegion<T>>();
    }

    /// <summary>
    /// Returns all data regions in the store.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DataRegion<T>> GetAllDataRegions()
    {
        return _tree.Search();
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
        return _tree.Search(env);
    }

    public IEnumerable<T> GetData(int row, int col)
    {
        return GetDataRegions(row, col).Select(x => x.Data);
    }

    /// <summary>
    /// Returns the data regions that are contained inside the region.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    internal IEnumerable<DataRegion<T>> GetContainedRegions(IRegion region)
    {
        return _tree.Search(region.ToEnvelope())
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
    /// Updates region positions by handling the row insert. Regions are shifted/expanded appropriately.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="nRows"></param>
    /// <param name="expandNeighbouring">Whether to expand the neighbouring values. If null, the value set on the class is used.</param>
    public void InsertRows(int rowIndex, int nRows, bool? expandNeighbouring = null) =>
        InsertRowsOrColumnAndShift(rowIndex, nRows, Axis.Row, expandNeighbouring);

    /// <summary>
    /// Updates region positions by handling the col insert. Regions are shifted/expanded appropriately.
    /// </summary>
    /// <param name="colIndex"></param>
    /// <param name="nCols"></param>y
    /// <param name="expandNeighbouring">Whether to expand the neighbouring values. If null, the value set on the class is used.</param>
    public void InsertCols(int colIndex, int nCols, bool? expandNeighbouring = null) =>
        InsertRowsOrColumnAndShift(colIndex, nCols, Axis.Col, expandNeighbouring);

    private void InsertRowsOrColumnAndShift(int index, int nRowsOrCol, Axis axis, bool? expandNeighbouring)
    {
        var expand = expandNeighbouring ?? _expandWhenInsertAfter;

        // As an example for inserting rows, there are three things that can happen
        // 1. Any regions that intersect the index should be expanded by nRows
        // 2. If _expandWhenInsertedAfter = true, then when the bottom of any region is just above index, expand it too.
        // 3. Any regions below the index should be shifted down
        var i0 = expand ? index - 1 : index;
        IRegion region = axis == Axis.Col ? new ColumnRegion(i0, index) : new RowRegion(i0, index);
        var intersecting = GetDataRegions(region);
        var dataRegionsToAdd = new List<DataRegion<T>>();

        foreach (var r in intersecting)
        {
            if (r.Region.GetLeadingEdgeOffset(axis) == index)
                continue; // we shift in this case, and don't expand
            var i1 = r.Region.GetTrailingEdgeOffset(axis);
            if (!expand && index > i1)
                continue;
            var clonedRegion = r.Region.Clone();
            clonedRegion.Expand(axis == Axis.Row ? Edge.Bottom : Edge.Right, nRowsOrCol);
            dataRegionsToAdd.Add(new DataRegion<T>(r.Data, clonedRegion));
            _tree.Delete(r);
        }

        // index - 1 because the top of the region has to be above the region to shift it down
        var below = this.GetAfter(index - 1, axis);
        foreach (var r in below)
        {
            var clonedRegion = r.Region.Clone();
            var dRow = axis == Axis.Row ? nRowsOrCol : 0;
            var dCol = axis == Axis.Col ? nRowsOrCol : 0;
            clonedRegion.Shift(dRow, dCol);
            dataRegionsToAdd.Add(new DataRegion<T>(r.Data, clonedRegion));
            _tree.Delete(r);
        }

        foreach (var dr in dataRegionsToAdd)
        {
            _tree.Insert(dr);
        }
    }

    /// <summary>
    /// Updates the region positions by handling row deletes. Regions are shifted/contracted appropriately.
    /// Returns any data that is removed during this operation.
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <returns>Any data that is removed during this operation</returns>
    public RegionRestoreData<T> RemoveRows(int rowStart, int rowEnd) =>
        RemoveRowsOrColumsAndShift(rowStart, rowEnd, Axis.Row);

    /// <summary>
    /// Updates the region positions by handling column deletes. Regions are shifted/contracted appropriately.
    /// Returns any data that is removed during this operation.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <returns>Any data that is removed during this operation</returns>
    public RegionRestoreData<T> RemoveCols(int colStart, int colEnd) =>
        RemoveRowsOrColumsAndShift(colStart, colEnd, Axis.Col);

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
        var removed = new List<DataRegion<T>>();
        var overlapping = GetDataRegions(region);
        var newDataRegions = new List<DataRegion<T>>();

        foreach (var r in overlapping)
        {
            // If we are leaving the region with an area less than or equal to min, remove.
            // For the example of removing rows, consider a rect with a height 3 and width 1
            // if we remove top two rows there is a width of 1 and if the min area is less than one, it should be removed.
            var cuts = r.Region.Break(region);
            var cutArea = cuts.Sum(x => x.Area);
            if (cutArea <= _minArea)
            {
                _tree.Delete(r);
                removed.Add(r);
                continue;
            }

            // Contract regions that intersect
            var nOverlapping = r.Region.GetIntersection(region)!.GetSize(axis);
            var clonedRegion = r.Region.Clone();
            clonedRegion.Contract(axis == Axis.Row ? Edge.Bottom : Edge.Right, nOverlapping);
            newDataRegions.Add(new DataRegion<T>(r.Data, clonedRegion));
            _tree.Delete(r);

            // There's a special case that if we remove from the top/left of a region, then it becomes un-recoverable
            // because inserting at the top index won't expand but rather will shift down. So store this region as "removed"
            var r0 = axis == Axis.Col ? r.Region.Left : r.Region.Top;
            if (r0 == start)
                removed.Add(r);

            // This also applies if we remove from the bottom/right of a region,
            // but only if _expandWhenInsertAfter = false
            if (!_expandWhenInsertAfter)
            {
                var r1 = axis == Axis.Col ? r.Region.Right : r.Region.Bottom;
                if (r1 == start)
                    removed.Add(r);
            }
        }

        // shift rights right/below
        var next = GetAfter(end, axis);
        foreach (var dataRegion in next)
        {
            _tree.Delete(dataRegion);
            var copiedRegion = dataRegion.Region.Clone();
            var nRows = axis == Axis.Row ? (end - start) + 1 : 0;
            var nCols = axis == Axis.Col ? (end - start) + 1 : 0;
            copiedRegion.Shift(-nRows, -nCols);
            newDataRegions.Add(new DataRegion<T>(dataRegion.Data, copiedRegion));
        }

        _tree.BulkLoad(newDataRegions);

        return new RegionRestoreData<T>()
        {
            RegionsRemoved = removed
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
                    .Where(x => x.Region.Top > rowOrCol);
            case Axis.Col:
                return this.GetDataRegions(new ColumnRegion(rowOrCol + 1, int.MaxValue))
                    .Where(x => x.Region.Left > rowOrCol);
        }

        return Enumerable.Empty<DataRegion<T>>();
    }

    /// <summary>
    /// Gets data to the left/above the given row or column.
    /// </summary>
    /// <param name="rowOrCol"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    private IEnumerable<DataRegion<T>> GetBefore(int rowOrCol, Axis axis)
    {
        switch (axis)
        {
            case Axis.Row:
                return this.GetDataRegions(new RowRegion(0, rowOrCol - 1))
                    .Where(x => x.Region.Bottom < rowOrCol);
            case Axis.Col:
                return this.GetDataRegions(new ColumnRegion(0, rowOrCol - 1))
                    .Where(x => x.Region.Right < rowOrCol);
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
    public virtual (List<IRegion> regionsRemoved, List<IRegion> regionsAdded) Clear(IRegion region, T data)
    {
        var overlapping = GetDataRegions(region)
            .Where(x => x.Data.Equals(data));

        if (!overlapping.Any())
            return (new List<IRegion>(), new List<IRegion>());

        var dataRegionsToRemove = new List<DataRegion<T>>();
        var dataRegionsToAdd = new List<DataRegion<T>>();

        foreach (var overlap in overlapping)
        {
            dataRegionsToRemove.Add(overlap);
            if (region.Contains(overlap.Region))
                continue;

            var breakRegions = overlap.Region.Break(region);
            dataRegionsToAdd.AddRange(breakRegions.Select(x => new DataRegion<T>(overlap.Data, x)));
        }

        foreach (var regionToRemove in dataRegionsToRemove)
            _tree.Delete(regionToRemove);

        _tree.BulkLoad(dataRegionsToAdd);

        return (dataRegionsToRemove.Select(x => x.Region).ToList(),
            dataRegionsToAdd.Select(x => x.Region).ToList());
    }

    /// <summary>
    /// Adds a data region to the store.
    /// Overlapping regions are not removed.
    /// </summary>
    /// <param name="dataRegion"></param>
    internal virtual RegionRestoreData<T> Add(DataRegion<T> dataRegion)
    {
        _tree.Insert(dataRegion);
        return new RegionRestoreData<T>();
    }

    public void Delete(DataRegion<T> dataRegion)
    {
        _tree.Delete(dataRegion);
    }

    public bool Any() => _tree.Count > 0;

    public bool Any(int row, int col)
    {
        return GetDataRegions(row, col).Any();
    }

    public void AddRange(List<DataRegion<T>> dataRegions)
    {
        _tree.BulkLoad(dataRegions);
    }

    public virtual void Restore(RegionRestoreData<T> restoreData)
    {
        foreach (var added in restoreData.RegionsAdded)
        {
            _tree.Delete(added);
        }

        _tree.BulkLoad(restoreData.RegionsRemoved);
    }
}