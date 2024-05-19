using System.Collections;
using System.Data.SqlTypes;
using System.Net;
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
        var expand = expandNeighbouring ?? ExpandWhenInsertAfter;

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
            Tree.Delete(r);
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
            Tree.Delete(r);
        }

        foreach (var dr in dataRegionsToAdd)
        {
            Tree.Insert(dr);
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
            if (cutArea <= MinArea)
            {
                Tree.Delete(r);
                removed.Add(r);
                continue;
            }

            // Contract regions that intersect only if the rows/cols are contained inside the region
            // The restore will then just be handled by re-inserting rows/cols which will expand the regions.
            var contained = axis == Axis.Col
                ? (r.Region.SpansCol(start + 1) && r.Region.SpansCol(end - 1))
                : (r.Region.SpansRow(start + 1) && r.Region.SpansRow(end - 1));

            if (contained)
            {
                var nOverlapping = r.Region.GetIntersection(region)!.GetSize(axis);
                var clonedRegion = r.Region.Clone();
                clonedRegion.Contract(axis == Axis.Row ? Edge.Bottom : Edge.Right, nOverlapping);
                newDataRegions.Add(new DataRegion<T>(r.Data, clonedRegion));
                Tree.Delete(r);
            }
            else // Add the bits that aren't intersecting back in only
            {
                // we need to shift anything to the left/up if it is located to the right/bottom
                // of the remove row/col
                foreach (var cut in cuts)
                {
                    var dCol = (axis == Axis.Col && cut.Left >= end) ? (start - end + 1) : 0;
                    var dRow = (axis == Axis.Row && cut.Top >= end) ? (start - end + 1) : 0;
                    cut.Shift(-dRow, -dCol);
                    newDataRegions.Add(new DataRegion<T>(r.Data, cut));
                }

                removed.Add(r);
                Tree.Delete(r);
            }
        }

        // shift rights right/below
        var next = GetAfter(end, axis);
        foreach (var dataRegion in next)
        {
            Tree.Delete(dataRegion);
            var copiedRegion = dataRegion.Region.Clone();
            var nRows = axis == Axis.Row ? (end - start) + 1 : 0;
            var nCols = axis == Axis.Col ? (end - start) + 1 : 0;
            copiedRegion.Shift(-nRows, -nCols);
            newDataRegions.Add(new DataRegion<T>(dataRegion.Data, copiedRegion));
        }

        Tree.BulkLoad(newDataRegions);

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
    /// Gets data regions inside the <paramref name="region"/> given, limited to those regions
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

    public void Delete(DataRegion<T> dataRegion)
    {
        Tree.Delete(dataRegion);
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
        foreach (var added in restoreData.RegionsAdded)
        {
            Tree.Delete(added);
        }

        Tree.BulkLoad(restoreData.RegionsRemoved);
    }
}