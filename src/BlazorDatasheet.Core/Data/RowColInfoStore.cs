using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public abstract class RowColInfoStore
{
    private readonly Sheet _sheet;
    public double DefaultSize { get; }

    private readonly Range1DStore<string> _headingStore = new(null);

    // stores and manages the *cumulative* sizes, for visual purposes.
    protected readonly CumulativeRange1DStore CumulativeSizeStore;

    // stores the individual sizes. This may be different to cumulative,
    // since cumulative includes 0 sizes when rows/columns are invisible.
    protected readonly Range1DStore<double> SizeStore;

    /// <summary>
    /// Stores formats that apply to the whole row/column
    /// </summary>
    internal readonly MergeableIntervalStore<CellFormat> Formats = new();

    /// <summary>
    /// Stores whether each row/column is visible
    /// </summary>
    private readonly Range1DStore<bool> _visible = new(true);

    private readonly Axis _axis;

    protected RowColInfoStore(double defaultSize, Sheet sheet, Axis axis)
    {
        _sheet = sheet;
        DefaultSize = defaultSize;
        CumulativeSizeStore = new CumulativeRange1DStore(defaultSize);
        SizeStore = new Range1DStore<double>(defaultSize);
        _axis = axis;
    }

    /// <summary>
    /// Returns either a RowRegion or ColumnRegion depending on the axis
    /// </summary>
    /// <param name="start">The start row/column of the region, along of the axis</param>
    /// <param name="end">The end row/column of the region, along of the axis</param>
    /// <returns></returns>
    protected IRegion GetSpannedRegion(int start, int end)
    {
        return _axis == Axis.Col ? new ColumnRegion(start, end) : new RowRegion(start, end);
    }

    /// <summary>
    /// Fired when a row/column is inserted into the sheet
    /// </summary>
    public virtual event EventHandler<RowColInsertedEventArgs>? Inserted;

    /// <summary>
    /// Fired when a row/column is removed from the sheet.
    /// </summary>
    public virtual event EventHandler<RowColRemovedEventArgs>? Removed;

    /// <summary>
    /// Fired when a row/column size is changed.
    /// </summary>
    public virtual event EventHandler<SizeModifiedEventArgs>? SizeModified;

    /// <summary>
    /// Sets the sizes of rows/cols between (and including) the indices specified, to the value given.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    internal RowColInfoRestoreData SetSizesImpl(int start, int end, double size)
    {
        var restoreData = new RowColInfoRestoreData()
        {
            CumulativeSizesModified = CumulativeSizeStore.Set(start, end, size),
            SizesModified = SizeStore.Set(start, end, size)
        };

        SizeModified?.Invoke(this, new SizeModifiedEventArgs(start, end, _axis));
        return restoreData;
    }

    /// <summary>
    /// Sets the headings of all rows between (and including) rows specified, to the value given.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal RowColInfoRestoreData SetHeadingsImpl(int start, int end, string heading)
    {
        var restoreData = _headingStore.Set(start, end, heading);
        _sheet.MarkDirty(GetSpannedRegion(start, end));
        return new RowColInfoRestoreData()
        {
            HeadingsModifed = restoreData
        };
    }

    /// <summary>
    /// Removes the rows/columns between (and including) the indexes given.
    /// Handles shifting the indices to the up/left and returns any modified data.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    internal RowColInfoRestoreData RemoveImpl(int start, int end)
    {
        var res = new RowColInfoRestoreData()
        {
            CumulativeSizesModified = CumulativeSizeStore.Delete(start, end),
            SizesModified = SizeStore.Delete(start, end),
            HeadingsModifed = _headingStore.Delete(start, end),
            RowFormatRestoreData = new RowColFormatRestoreData()
            {
                IntervalsRemoved = Formats.Clear(start, end)
            }
        };

        Formats.ShiftLeft(start, (end - start) + 1);
        _sheet.MarkDirty(GetSpannedRegion(start, _sheet.GetSize(_axis)));
        Removed?.Invoke(this, new RowColRemovedEventArgs(start, (end - start) + 1, _axis));
        return res;
    }

    internal RowColInfoRestoreData UnhideImpl(int start, int count)
    {
        var restoreData = new RowColInfoRestoreData()
        {
            VisibilityModified = _visible.Clear(start, start + count - 1)
        };
        // We need to set the sizes in the cumulative store to the stored physical
        // sizes, since the cumulative store will have 0 size for hidden indices.
        var sizes = SizeStore.GetOverlapping(start, start + count - 1);
        // first set cumulative sizes in the range to default
        restoreData.CumulativeSizesModified.AddRange(
            CumulativeSizeStore.Set(start, start + count, DefaultSize)
        );

        foreach (var size in sizes)
        {
            restoreData.CumulativeSizesModified.AddRange(CumulativeSizeStore.Set(size.start, size.end,
                size.value));
        }

        _sheet.MarkDirty(GetSpannedRegion(start, start + count - 1));

        return restoreData;
    }

    internal RowColInfoRestoreData HideImpl(int start, int count)
    {
        var changedVisibility = _visible.Set(start, start + count - 1, false);
        var changedCumulativeSizes = CumulativeSizeStore.Set(start, start + count - 1, 0);

        SizeModified?.Invoke(this, new SizeModifiedEventArgs(start, start + count - 1, _axis));
        _sheet.MarkDirty(GetSpannedRegion(start, start + count - 1));
        return new RowColInfoRestoreData()
        {
            VisibilityModified = changedVisibility,
            CumulativeSizesModified = changedCumulativeSizes
        };
    }

    public bool IsVisible(int index)
    {
        return _visible.Get(index);
    }

    public void Hide(int start, int count)
    {
        var cmd = new HideCommand(start, count, _axis);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    public void Unhide(int start, int count)
    {
        var cmd = new UnhideCommand(start, count, _axis);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Returns the next visible row or column after the index given. If none found, returns -1.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="direction">If the direction is 1, returns the row/column after the index. If it is -1,
    /// returns the row/colum before the index.</param>
    /// <returns></returns>
    public int GetNextVisible(int index, int direction = 1)
    {
        if (direction == 0)
            return index;

        direction = Math.Abs(direction) / direction;

        var nextNonVisibleInterval = _visible.GetNext(index, direction);
        int nextIndex = index + direction;
        if (_visible.Get(nextIndex))
            return (nextIndex >= _sheet.GetSize(_axis) || nextIndex < 0 ? -1 : nextIndex);

        while (nextNonVisibleInterval != null)
        {
            if (direction == 1)
                nextIndex = nextNonVisibleInterval.Value.end + direction;
            else
                nextIndex = nextNonVisibleInterval.Value.position + direction;

            if (_visible.Get(nextIndex))
                break;

            nextNonVisibleInterval = _visible.GetNext(nextIndex, direction);
        }

        if (nextIndex >= _sheet.GetSize(_axis) || nextIndex < 0)
            return -1;

        return nextIndex;
    }

    /// <summary>
    /// Inserts n empty rows/columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    internal void InsertImpl(int start, int count)
    {
        CumulativeSizeStore.InsertAt(start, count);
        SizeStore.InsertAt(start, count);
        _headingStore.InsertAt(start, count);
        Formats.ShiftRight(start, count);
        Inserted?.Invoke(this, new RowColInsertedEventArgs(start, count, _axis));
        _sheet.MarkDirty(GetSpannedRegion(start, _sheet.GetSize(_axis)));
    }

    /// <summary>
    /// Returns the heading at the index given
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public string? GetHeading(int index)
    {
        return _headingStore.Get(index);
    }

    internal void Restore(RowColInfoRestoreData data)
    {
        CumulativeSizeStore.BatchSet(data.CumulativeSizesModified);
        SizeStore.BatchSet(data.SizesModified);

        _headingStore.BatchSet(data.HeadingsModifed);

        foreach (var originalData in data.VisibilityModified)
        {
            if (originalData.visibile)
                _visible.Clear(originalData.start, originalData.end);
            else
                _visible.Set(originalData.start, originalData.end, false);
        }

        foreach (var added in data.RowFormatRestoreData.IntervalsAdded)
            Formats.Clear(added);
        Formats.AddRange(data.RowFormatRestoreData.IntervalsRemoved);

        foreach (var change in data.CumulativeSizesModified)
            SizeModified?.Invoke(this, new SizeModifiedEventArgs(change.start, change.end, _axis));
    }

    internal RowColFormatRestoreData SetFormatImpl(CellFormat cellFormat, int start, int end)
    {
        var spanningRegion = GetSpannedRegion(start, end);
        // Keep track of individual cell changes
        var cellChanges = new List<CellStoreRestoreData>();

        // we will ALWAYS merge the row/column regardless of what the cells are doing.
        var newOi = new OrderedInterval<CellFormat>(start, end, cellFormat.Clone());
        var modified = Formats.Add(newOi);

        // We need to find the merges between the new region and the row formats/cell formats and add those as cell formats.
        // this is because we the order of choosing the cell format is 1. cell format, then 2. col format then 3. row format.
        // if we set col format then a row format with some intersection, we would find that the col format is chosen when we
        // query the format at the intersection. It should be the cell format, so we set that.
        var altAxisStore = _sheet.GetRowColStore(_axis == Axis.Col ? Axis.Row : Axis.Col);
        var colOverlaps = altAxisStore.Formats.GetAllIntervals()
            .Select(x =>
            {
                if (_axis == Axis.Col)
                    return new DataRegion<CellFormat>(x.Data, new Region(x.Start, x.End, start, end));
                else
                    return new DataRegion<CellFormat>(x.Data, new Region(start, end, x.Start, x.End));
            });

        var cellOverlaps = _sheet.Cells.GetFormatData(spanningRegion)
            .Select(x => new DataRegion<CellFormat>(x.Data, x.Region.GetIntersection(spanningRegion)!));

        // The intersectings region should be be merged with any existing (or empty) cell formats
        // So that the new, most recently applied format info is taken when the format is queried.
        // There may be some cell formats inside the col/row intersections in which case the format will be merged twice.
        // That should be ok because they will already exist and won't be added
        foreach (var overlap in colOverlaps.Concat(cellOverlaps))
        {
            cellChanges.Add(_sheet.Cells.MergeFormatImpl(overlap.Region, cellFormat));
        }

        _sheet.MarkDirty(spanningRegion);

        return new RowColFormatRestoreData()
        {
            CellFormatRestoreData = cellChanges,
            IntervalsAdded = new List<OrderedInterval<CellFormat>>() { newOi },
            IntervalsRemoved = modified
        };
    }

    public bool RemoveAt(int index, int count = 1)
    {
        var cmd = new RemoveRowColsCommand(index, _axis, count);
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the headings from (and including) <paramref name="indexStart"/> to <paramref name="indexEnd"/> to <paramref name="heading"/>
    /// </summary>
    /// <param name="indexStart"></param>
    /// <param name="indexEnd"></param>
    /// <param name="heading"></param>
    public void SetHeadings(int indexStart, int indexEnd, string heading)
    {
        _sheet.Commands.ExecuteCommand(new SetHeadingsCommand(indexStart, indexEnd, heading, _axis));
    }

    /// <summary>
    /// Inserts a row/column at an index specified.
    /// </summary>
    /// <param name="index">The index that the new row/column will be at. The new row/column will have the index specified.</param>
    /// <param name="count"></param>
    public void InsertAt(int index, int count = 1)
    {
        var indexToAddAt = Math.Min(_sheet.GetSize(_axis) - 1, Math.Max(index, 0));
        var cmd = new InsertRowsColsCommand(indexToAddAt, count, _axis);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the size between the two indices given (inclusive) to the size given (in px).
    /// </summary>
    /// <param name="indexStart"></param>
    /// <param name="indexEnd"></param>
    /// <param name="sizePx"></param>
    public void SetSize(int indexStart, int indexEnd, double sizePx)
    {
        var cmd = new SetSizeCommand(indexStart, indexEnd, sizePx, _axis);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the size at <paramref name="index"/> to the size given (in px).
    /// </summary>
    /// <param name="index"></param>
    /// <param name="sizePx"></param>
    public void SetSize(int index, double sizePx) => SetSize(index, index, sizePx);
}

internal class RowColInfoRestoreData
{
    public List<(int start, int end, double size)> CumulativeSizesModified { get; init; } = new();
    public List<(int start, int end, double size)> SizesModified { get; init; } = new();
    public List<(int start, int end, string heading)> HeadingsModifed { get; init; } = new();
    public List<(int start, int end, bool visibile)> VisibilityModified { get; init; } = new();
    public RowColFormatRestoreData RowFormatRestoreData { get; init; } = new();

    public void Merge(RowColInfoRestoreData other)
    {
        CumulativeSizesModified.AddRange(other.CumulativeSizesModified);
        HeadingsModifed.AddRange(other.HeadingsModifed);
        VisibilityModified.AddRange(other.VisibilityModified);
        RowFormatRestoreData.Merge(other.RowFormatRestoreData);
    }
}

internal class RowColFormatRestoreData
{
    internal List<CellStoreRestoreData> CellFormatRestoreData { get; set; } = new();
    internal List<OrderedInterval<CellFormat>> IntervalsRemoved { get; set; } = new();
    internal List<OrderedInterval<CellFormat>> IntervalsAdded { get; set; } = new();

    internal void Merge(RowColFormatRestoreData other)
    {
        CellFormatRestoreData.AddRange(other.CellFormatRestoreData);
        IntervalsRemoved.AddRange(other.IntervalsRemoved);
        IntervalsAdded.AddRange(other.IntervalsAdded);
    }
}