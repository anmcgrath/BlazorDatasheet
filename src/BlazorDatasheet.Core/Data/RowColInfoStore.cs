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
    public readonly Sheet Sheet;
    public double DefaultSize { get; }

    internal readonly Range1DStore<string> HeadingStore = new(null);

    // stores and manages the *cumulative* sizes, for visual purposes.
    protected readonly CumulativeRange1DStore CumulativeSizeStore;

    // stores the individual sizes. This may be different to cumulative,
    // since cumulative includes 0 sizes when rows/columns are invisible.
    internal readonly Range1DStore<double> SizeStore;

    /// <summary>
    /// Stores formats that apply to the whole row/column
    /// </summary>
    internal readonly MergeableIntervalStore<CellFormat> Formats = new();

    /// <summary>
    /// The first visible row/column.
    /// </summary>
    public int FirstVisible => GetNextVisible(-1, 1);

    /// <summary>
    /// Stores whether each row/column is visible.
    /// The default is true, if the row/colum is NOT visible, there will be
    /// data in the store for that index.
    /// </summary>
    internal readonly Range1DStore<bool> Visible = new(true);

    private readonly Axis _axis;

    protected RowColInfoStore(double defaultSize, Sheet sheet, Axis axis)
    {
        Sheet = sheet;
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
    private IRegion GetSpannedRegion(int start, int end)
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
            CumulativeSizesRestoreData = CumulativeSizeStore.Set(start, end, size),
            SizesRestoreData = SizeStore.Set(start, end, size)
        };

        EmitSizeModified(start, end);
        return restoreData;
    }

    protected void EmitSizeModified(int start, int end)
    {
        SizeModified?.Invoke(this, new SizeModifiedEventArgs(start, end, _axis));
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
        var restoreData = HeadingStore.Set(start, end, heading);
        Sheet.MarkDirty(GetSpannedRegion(start, end));
        return new RowColInfoRestoreData()
        {
            HeadingsRestoreData = restoreData
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
        var restoreData = new RowColInfoRestoreData()
        {
            CumulativeSizesRestoreData = CumulativeSizeStore.Delete(start, end),
            SizesRestoreData = SizeStore.Delete(start, end),
            HeadingsRestoreData = HeadingStore.Delete(start, end),
            VisibilityRestoreData = Visible.Delete(start, end),
            RowColFormatRestoreData = new RowColFormatRestoreData()
            {
                Format1DRestoreData = Formats.Clear(start, end)
            }
        };

        restoreData.RowColFormatRestoreData.Format1DRestoreData.Merge(Formats.ShiftLeft(start, (end - start) + 1));
        Sheet.MarkDirty(GetSpannedRegion(start, Sheet.GetSize(_axis)));
        EmitRemoved(start, (end - start) + 1);
        return restoreData;
    }

    internal RowColInfoRestoreData UnhideImpl(int start, int count)
    {
        return UnhideImpl([new(start, start + count - 1)]);
    }

    internal RowColInfoRestoreData UnhideImpl(IEnumerable<Interval> intervals)
    {
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;

        var restoreData = new RowColInfoRestoreData();

        foreach (var interval in intervals)
        {
            minRow = Math.Min(interval.Start, minRow);
            maxRow = Math.Max(interval.End, maxRow);

            restoreData.VisibilityRestoreData.Merge(Visible.Clear(interval.Start, interval.End));
            restoreData.CumulativeSizesRestoreData.Merge(CumulativeSizeStore.Set(interval.Start, interval.End,
                DefaultSize));

            // We need to set the sizes in the cumulative store to the stored physical
            // sizes, since the cumulative store will have 0 size for hidden indices.
            var sizes = SizeStore.GetOverlapping(interval.Start, interval.End);
            // first set cumulative sizes in the range to default

            foreach (var size in sizes)
            {
                restoreData.CumulativeSizesRestoreData.Merge(CumulativeSizeStore.Set(size.start, size.end,
                    size.value));
            }
        }

        Sheet.MarkDirty(GetSpannedRegion(0, int.MaxValue));
        EmitSizeModified(minRow, maxRow);

        return restoreData;
    }

    internal RowColInfoRestoreData HideImpl(int start, int count)
    {
        return HideImpl([new(start, start + count - 1)]);
    }

    internal RowColInfoRestoreData HideImpl(IEnumerable<Interval> intervals)
    {
        var restoreData = new RowColInfoRestoreData();
        Sheet.BatchUpdates();

        int minRow = int.MaxValue;
        int maxRow = int.MinValue;

        foreach (var interval in intervals)
        {
            minRow = Math.Min(interval.Start, minRow);
            maxRow = Math.Max(interval.End, maxRow);

            restoreData.VisibilityRestoreData.Merge(Visible.Set(interval.Start, interval.End, false));
            restoreData.CumulativeSizesRestoreData.Merge(CumulativeSizeStore.Set(interval.Start, interval.End, 0));
        }

        Sheet.MarkDirty(GetSpannedRegion(0, int.MaxValue));
        EmitSizeModified(minRow, maxRow);

        Sheet.EndBatchUpdates();
        return restoreData;
    }

    /// <summary>
    /// Returns whether the row/column is visible at <paramref name="index"/>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool IsVisible(int index)
    {
        if (index < 0 || index >= Sheet.GetSize(_axis))
            return false;
        return Visible.Get(index);
    }

    /// <summary>
    /// Counts the number of visible rows/columns between start+end
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public int CountVisible(int start, int end)
    {
        var totalCount = Math.Min(end - start + 1, Sheet.GetSize(_axis));
        int invisibleCount = 0;
        var invisible = Visible.GetOverlapping(start, end);
        foreach (var i in invisible)
        {
            var nOverlap = Math.Min(i.end, end) - Math.Max(i.start, start) + 1;
            invisibleCount += nOverlap;
        }

        return totalCount - invisibleCount;
    }

    public void Hide(int start, int count)
    {
        if (count == 0)
            return;

        var cmd = new HideCommand(start, start + count - 1, _axis);
        Sheet.Commands.ExecuteCommand(cmd);
    }

    public void Unhide(int start, int count)
    {
        if (count == 0)
            return;

        var cmd = new UnhideCommand(start, start + count - 1, _axis);
        Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Returns the visible rows or columns between <paramref name="start"/> and <paramref name="end"/> inclusive.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<int> GetVisibleIndices(int start, int end)
    {
        if (Sheet.GetSize(_axis) == 0)
            return new List<int>();

        start = Math.Max(start, 0);
        end = Math.Min(end, Sheet.GetSize(_axis) - 1);
        var hidden = Visible.GetOverlapping(start, end);

        if (!hidden.Any())
            return Enumerable.Range(start, end - start + 1).ToList();

        var visibleRows = new List<int>();

        int n = hidden.First().start - start;
        if (n > 0)
            visibleRows.AddRange(Enumerable.Range(start, n));

        for (int i = 0; i < hidden.Count - 1; ++i)
        {
            n = hidden[i + 1].start - (hidden[i].end + 1);
            if (n > 0)
                visibleRows.AddRange(Enumerable.Range(hidden[i].end + 1, n));
        }

        n = end - hidden.Last().end;
        if (n > 0)
            visibleRows.AddRange(Enumerable.Range(hidden.Last().end + 1, n));

        return visibleRows;
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

        var nextNonVisibleInterval = Visible.GetNext(index, direction);
        int nextIndex = index + direction;
        if (Visible.Get(nextIndex))
            return (nextIndex >= Sheet.GetSize(_axis) || nextIndex < 0 ? -1 : nextIndex);

        while (nextNonVisibleInterval != null)
        {
            if (direction == 1)
                nextIndex = nextNonVisibleInterval.Value.End + direction;
            else
                nextIndex = nextNonVisibleInterval.Value.Start + direction;

            if (Visible.Get(nextIndex) && !nextIndex.Equals(index))
                break;

            nextNonVisibleInterval = Visible.GetNext(nextIndex, direction);
        }

        if (nextNonVisibleInterval == null)
            nextIndex = direction == 1 ? Visible.End + 1 : Visible.Start - 1;

        if (nextIndex >= Sheet.GetSize(_axis) || nextIndex < 0)
            return -1;

        return nextIndex;
    }

    public IEnumerable<Interval> GetVisible()
    {
        return Visible.GetAllIntervals();
    }

    /// <summary>
    /// Inserts n empty rows/columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    internal RowColInfoRestoreData InsertImpl(int start, int count)
    {
        var restoreData = new RowColInfoRestoreData()
        {
            CumulativeSizesRestoreData = CumulativeSizeStore.InsertAt(start, count),
            SizesRestoreData = SizeStore.InsertAt(start, count),
            HeadingsRestoreData = HeadingStore.InsertAt(start, count),
            VisibilityRestoreData = Visible.InsertAt(start, count),
            RowColFormatRestoreData = new RowColFormatRestoreData()
            {
                Format1DRestoreData = Formats.ShiftRight(start, count)
            }
        };

        EmitInserted(start, count);
        Sheet.MarkDirty(GetSpannedRegion(start, Sheet.GetSize(_axis)));
        return restoreData;
    }

    /// <summary>
    /// Returns the heading at the index given
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public string? GetHeading(int index)
    {
        return HeadingStore.Get(index);
    }

    internal void Restore(RowColInfoRestoreData data)
    {
        CumulativeSizeStore.Restore(data.CumulativeSizesRestoreData);
        SizeStore.Restore(data.SizesRestoreData);
        HeadingStore.Restore(data.HeadingsRestoreData);
        Visible.Restore(data.VisibilityRestoreData);
        Formats.Restore(data.RowColFormatRestoreData.Format1DRestoreData);

        foreach (var change in data.CumulativeSizesRestoreData.RemovedIntervals.Concat(data.CumulativeSizesRestoreData
                     .AddedIntervals))
        {
            EmitSizeModified(change.Start, change.End);
        }
    }

    internal void EmitInserted(int start, int count)
    {
        Inserted?.Invoke(this, new RowColInsertedEventArgs(start, count, _axis));
    }

    internal void EmitRemoved(int start, int count)
    {
        Removed?.Invoke(this, new RowColRemovedEventArgs(start, count, _axis));
    }

    internal RowColFormatRestoreData SetFormatImpl(CellFormat cellFormat, int start, int end)
    {
        var spanningRegion = GetSpannedRegion(start, end);
        // Keep track of individual cell changes
        var cellChanges = new List<CellStoreRestoreData>();

        // we will ALWAYS merge the row/column regardless of what the cells are doing.
        var newOi = new OrderedInterval<CellFormat>(start, end, cellFormat.Clone());
        var format1DRestoreData = Formats.Add(newOi);

        // We need to find the merges between the new region and the row formats/cell formats and add those as cell formats.
        // this is because we the order of choosing the cell format is 1. cell format, then 2. col format then 3. row format.
        // if we set col format then a row format with some intersection, we would find that the col format is chosen when we
        // query the format at the intersection. It should be the cell format, so we set that.
        var altAxisStore = Sheet.GetRowColStore(_axis == Axis.Col ? Axis.Row : Axis.Col);
        var colOverlaps = altAxisStore.Formats.GetAllIntervals()
            .Select(x =>
            {
                if (_axis == Axis.Col)
                    return new DataRegion<CellFormat>(x.Data, new Region(x.Start, x.End, start, end));
                else
                    return new DataRegion<CellFormat>(x.Data, new Region(start, end, x.Start, x.End));
            });

        var cellOverlaps = Sheet.Cells.GetFormatData(spanningRegion)
            .Select(x => new DataRegion<CellFormat>(x.Data, x.Region.GetIntersection(spanningRegion)!));

        // The intersectings region should be be merged with any existing (or empty) cell formats
        // So that the new, most recently applied format info is taken when the format is queried.
        // There may be some cell formats inside the col/row intersections in which case the format will be merged twice.
        // That should be ok because they will already exist and won't be added
        foreach (var overlap in colOverlaps.Concat(cellOverlaps))
        {
            cellChanges.Add(Sheet.Cells.MergeFormatImpl(overlap.Region, cellFormat));
        }

        Sheet.MarkDirty(spanningRegion);

        return new RowColFormatRestoreData()
        {
            CellFormatRestoreData = cellChanges,
            Format1DRestoreData = format1DRestoreData
        };
    }

    public bool RemoveAt(int index, int count = 1)
    {
        var cmd = new RemoveRowColsCommand(index, _axis, count);
        return Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the headings from (and including) <paramref name="indexStart"/> to <paramref name="indexEnd"/> to <paramref name="heading"/>
    /// </summary>
    /// <param name="indexStart"></param>
    /// <param name="indexEnd"></param>
    /// <param name="heading"></param>
    public void SetHeadings(int indexStart, int indexEnd, string heading)
    {
        Sheet.Commands.ExecuteCommand(new SetHeadingsCommand(indexStart, indexEnd, heading, _axis));
    }

    /// <summary>
    /// Inserts a row/column at an index specified.
    /// </summary>
    /// <param name="index">The index that the new row/column will be at. The new row/column will have the index specified.</param>
    /// <param name="count"></param>
    public void InsertAt(int index, int count = 1)
    {
        var indexToAddAt = Math.Min(Sheet.GetSize(_axis), Math.Max(index, 0));
        var cmd = new InsertRowsColsCommand(indexToAddAt, count, _axis);
        Sheet.Commands.ExecuteCommand(cmd);
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
        Sheet.Commands.ExecuteCommand(cmd);
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
    public MergeableIntervalStoreRestoreData<OverwritingValue<double>> CumulativeSizesRestoreData { get; init; } =
        new();

    public MergeableIntervalStoreRestoreData<OverwritingValue<double>> SizesRestoreData { get; init; } = new();
    public MergeableIntervalStoreRestoreData<OverwritingValue<string>> HeadingsRestoreData { get; init; } = new();
    public MergeableIntervalStoreRestoreData<OverwritingValue<bool>> VisibilityRestoreData { get; init; } = new();
    public RowColFormatRestoreData RowColFormatRestoreData { get; init; } = new();

    public void Merge(RowColInfoRestoreData other)
    {
        CumulativeSizesRestoreData.Merge(other.CumulativeSizesRestoreData);
        HeadingsRestoreData.Merge(other.HeadingsRestoreData);
        VisibilityRestoreData.Merge(other.VisibilityRestoreData);
        RowColFormatRestoreData.Merge(other.RowColFormatRestoreData);
    }
}

internal class RowColFormatRestoreData
{
    internal List<CellStoreRestoreData> CellFormatRestoreData { get; set; } = new();
    internal MergeableIntervalStoreRestoreData<CellFormat> Format1DRestoreData { get; set; } = new();

    internal void Merge(RowColFormatRestoreData other)
    {
        CellFormatRestoreData.AddRange(other.CellFormatRestoreData);
        Format1DRestoreData.Merge(other.Format1DRestoreData);
    }
}