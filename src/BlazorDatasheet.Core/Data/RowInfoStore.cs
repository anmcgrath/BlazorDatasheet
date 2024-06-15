using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public class RowInfoStore
{
    private readonly Sheet _sheet;
    public double DefaultHeight { get; }

    private readonly Range1DStore<string> _headingStore = new(null);

    // stores and manages the cumulative heights of the rows
    private readonly CumulativeRange1DStore _cumulativeHeightStore;

    //  stores the individual row heights. This may be different to cumulative,
    // since cumulative includes 0 heights when rows are invisible.
    private readonly Range1DStore<double> _heightStore;
    internal readonly MergeableIntervalStore<CellFormat> RowFormats = new();
    private readonly Range1DStore<bool> _visibleRows = new(true);

    /// <summary>
    /// Fired when a row is inserted into the sheet
    /// </summary>
    public event EventHandler<RowInsertedEventArgs>? RowInserted;

    /// <summary>
    /// Fired when a row is removed from the sheet.
    /// </summary>
    public event EventHandler<RowRemovedEventArgs>? RowRemoved;

    /// <summary>
    /// Fired when a row height is changed.
    /// </summary>
    public event EventHandler<RowHeightChangedEventArgs>? RowHeightChanged;

    public RowInfoStore(double defaultHeight, Sheet sheet)
    {
        _sheet = sheet;
        DefaultHeight = defaultHeight;
        _cumulativeHeightStore = new CumulativeRange1DStore(defaultHeight);
        _heightStore = new Range1DStore<double>(defaultHeight);
    }

    /// <summary>
    /// Sets the row heights of all rows between (and including) the rows specified, to the value given.
    /// Returns any row ranges that were modified.
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    internal RowInfoStoreRestoreData SetRowHeightsImpl(int rowStart, int rowEnd, double height)
    {
        var restoreData = new RowInfoStoreRestoreData()
        {
            CumulativeHeightsModified = _cumulativeHeightStore.Set(rowStart, rowEnd, height),
            HeightsModified = _heightStore.Set(rowStart, rowEnd, height)
        };

        RowHeightChanged?.Invoke(this, new RowHeightChangedEventArgs(rowStart, rowEnd));
        return restoreData;
    }

    /// <summary>
    /// Sets the headings of all rows between (and including) rows specified, to the value given.
    /// Returns any rows ranges that were modified.
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal List<(int start, int end, string heading)> SetRowHeadingsImpl(int rowStart, int rowEnd, string heading)
    {
        var restoreData = _headingStore.Set(rowStart, rowEnd, heading);
        _sheet.MarkDirty(new RowRegion(rowStart, rowEnd));
        return restoreData;
    }

    /// <summary>
    /// Removes the columns between (and including) the indexes given.
    /// Handles shifting the row indices to the left and returns any modified data.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    internal RowInfoStoreRestoreData RemoveRowsImpl(int start, int end)
    {
        var res = new RowInfoStoreRestoreData()
        {
            CumulativeHeightsModified = _cumulativeHeightStore.Delete(start, end),
            HeightsModified = _heightStore.Delete(start, end),
            HeadingsModifed = _headingStore.Delete(start, end),
            RowFormatRestoreData = new RowColFormatRestoreData()
            {
                IntervalsRemoved = RowFormats.Clear(start, end)
            }
        };

        RowFormats.ShiftLeft(start, (end - start) + 1);
        _sheet.MarkDirty(new RowRegion(start, _sheet.NumRows));
        RowRemoved?.Invoke(this, new RowRemovedEventArgs(start, (end - start) + 1));
        return res;
    }

    internal RowInfoStoreRestoreData UnhideRowsImpl(int start, int nRows)
    {
        var restoreData = new RowInfoStoreRestoreData()
        {
            VisibilityModified = _visibleRows.Clear(start, start + nRows - 1)
        };
        // We need to set the heights in the cumulative store to the stored physical rows
        // heights, since the cumulative store will have 0 heights for hidden rows.
        var heights = _heightStore.GetOverlapping(start, start + nRows - 1);
        // first set cumulative heights in the range to default
        restoreData.CumulativeHeightsModified.AddRange(
            _cumulativeHeightStore.Set(start, start + nRows, DefaultHeight)
        );

        foreach (var height in heights)
        {
            restoreData.CumulativeHeightsModified.AddRange(_cumulativeHeightStore.Set(height.start, height.end,
                height.value));
        }

        _sheet.MarkDirty(new RowRegion(start, start + nRows - 1));

        return restoreData;
    }

    internal RowInfoStoreRestoreData HideRowsImpl(int start, int nRows)
    {
        var changedVisibility = _visibleRows.Set(start, start + nRows - 1, false);
        var changedCumulativeHeights = _cumulativeHeightStore.Set(start, start + nRows - 1, 0);

        RowHeightChanged?.Invoke(this, new RowHeightChangedEventArgs(start, start + nRows - 1));
        _sheet.MarkDirty(new RowRegion(start, start + nRows - 1));
        return new RowInfoStoreRestoreData()
        {
            VisibilityModified = changedVisibility,
            CumulativeHeightsModified = changedCumulativeHeights
        };
    }

    public bool IsRowVisible(int row)
    {
        return _visibleRows.Get(row);
    }

    public void HideRows(int start, int nRows)
    {
        var cmd = new HideRowsCommand(start, nRows);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    public void UnhideRows(int start, int nRows)
    {
        var cmd = new UnhideRowsCommand(start, nRows);
        _sheet.Commands.ExecuteCommand(cmd);
    }


    /// <summary>
    /// Returns the next visible row. If no visible row is found, returns -1.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public int GetNextVisibleRow(int rowIndex, int direction = 1)
    {
        if (direction == 0)
            return rowIndex;

        direction = Math.Abs(direction) / direction;

        var nextNonVisibleInterval = _visibleRows.GetNext(rowIndex, direction);
        int index = rowIndex + direction;
        if (_visibleRows.Get(index))
            return (index >= _sheet.NumRows || index < 0 ? -1 : index);

        while (nextNonVisibleInterval != null)
        {
            if (direction == 1)
                index = nextNonVisibleInterval.Value.end + direction;
            else
                index = nextNonVisibleInterval.Value.position + direction;

            if (_visibleRows.Get(index))
                break;

            nextNonVisibleInterval = _visibleRows.GetNext(index, direction);
        }

        if (index >= _sheet.NumRows || index < 0)
            return -1;

        return index;
    }

    /// <summary>
    /// Inserts n empty rows.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    internal void InsertImpl(int start, int n)
    {
        _cumulativeHeightStore.InsertAt(start, n);
        _heightStore.InsertAt(start, n);
        _headingStore.InsertAt(start, n);
        RowFormats.ShiftRight(start, n);
        RowInserted?.Invoke(this, new RowInsertedEventArgs(start, n));
        _sheet.MarkDirty(new RowRegion(start, _sheet.NumRows));
    }

    /// <summary>
    /// Returns the heading at the row index
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public string? GetHeading(int rowIndex)
    {
        return _headingStore.Get(rowIndex);
    }

    /// <summary>
    /// Returns the row index at the position y
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetRow(double y)
    {
        return _cumulativeHeightStore.GetPosition(y);
    }

    /// <summary>
    /// Returns the height of the row specified. This is the visual height, so it will be 0 if the row is hidden.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public double GetVisualHeight(int row)
    {
        return _cumulativeHeightStore.GetSize(row);
    }

    /// <summary>
    /// Returns the physical height of the row. This is non-zero even if the row is
    /// hidden. For visual height, use <seealso cref="GetVisualHeight"/>
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public double GetPhysicalHeight(int row)
    {
        return _heightStore.Get(row);
    }

    /// <summary>
    /// Returns the distance between the top positions of two rows.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetHeightBetween(int start, int end)
    {
        return _cumulativeHeightStore.GetSizeBetween(start, end);
    }

    /// <summary>
    /// Returns the left position of the row index
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public double GetTop(int rowIndex)
    {
        return _cumulativeHeightStore.GetCumulative(rowIndex);
    }

    internal void Restore(RowInfoStoreRestoreData data)
    {
        _cumulativeHeightStore.BatchSet(data.CumulativeHeightsModified);
        _heightStore.BatchSet(data.HeightsModified);

        _headingStore.BatchSet(data.HeadingsModifed);

        foreach (var originalData in data.VisibilityModified)
        {
            if (originalData.visibile)
                _visibleRows.Clear(originalData.start, originalData.end);
            else
                _visibleRows.Set(originalData.start, originalData.end, false);
        }

        foreach (var added in data.RowFormatRestoreData.IntervalsAdded)
            RowFormats.Clear(added);
        RowFormats.AddRange(data.RowFormatRestoreData.IntervalsRemoved);

        foreach (var change in data.CumulativeHeightsModified)
            RowHeightChanged?.Invoke(this, new RowHeightChangedEventArgs(change.start, change.end));
    }

    public CellFormat? GetFormat(int row)
    {
        return RowFormats.Get(row);
    }

    internal RowColFormatRestoreData SetRowFormatImpl(CellFormat cellFormat, RowRegion rowRegion)
    {
        // Keep track of individual cell changes
        var cellChanges = new List<CellStoreRestoreData>();

        // we will ALWAYS merge the column regardless of what the cells are doing.
        var newOi = new OrderedInterval<CellFormat>(rowRegion.Top, rowRegion.Bottom, cellFormat.Clone());
        var modified = RowFormats.Add(newOi);

        // We need to find the merges between the new region and the row formats/cell formats and add those as cell formats.
        // this is because we the order of choosing the cell format is 1. cell format, then 2. col format then 3. row format.
        // if we set col format then a row format with some intersection, we would find that the col format is chosen when we
        // query the format at the intersection. It should be the cell format, so we set that.
        var colOverlaps = _sheet.Columns.ColFormats.GetAllIntervals()
            .Select(x =>
                new DataRegion<CellFormat>(x.Data, new Region(rowRegion.Top, rowRegion.Bottom, x.Start, x.End)));

        var cellOverlaps = _sheet.Cells.GetFormatData(rowRegion)
            .Select(x => new DataRegion<CellFormat>(x.Data, x.Region.GetIntersection(rowRegion)!));

        // The intersectings region should be be merged with any existing (or empty) cell formats
        // So that the new, most recently applied format info is taken when the format is queried.
        // There may be some cell formats inside the col/row intersections in which case the format will be merged twice.
        // That should be ok because they will already exist and won't be added
        foreach (var overlap in colOverlaps.Concat(cellOverlaps))
        {
            cellChanges.Add(_sheet.Cells.MergeFormatImpl(overlap.Region, cellFormat));
        }

        _sheet.MarkDirty(rowRegion);

        return new RowColFormatRestoreData()
        {
            CellFormatRestoreData = cellChanges,
            IntervalsAdded = new List<OrderedInterval<CellFormat>>() { newOi },
            IntervalsRemoved = modified
        };
    }

    public bool RemoveAt(int index, int nRows = 1)
    {
        var cmd = new RemoveRowsCommand(index, nRows);
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Inserts a row at an index specified.
    /// </summary>
    /// <param name="rowIndex">The index that the new row will be at. The new row will have the index specified.</param>
    public void InsertRowAt(int rowIndex, int nRows = 1)
    {
        var indexToAddAt = Math.Min(_sheet.NumRows - 1, Math.Max(rowIndex, 0));
        var cmd = new InsertRowsAtCommand(indexToAddAt, nRows);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the height of a row, to the height given (in px).
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="height"></param>
    public void SetHeight(int rowStart, int rowEnd, double height)
    {
        var cmd = new SetRowHeightCommand(rowStart, rowEnd, height);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the height of a row, to the height given (in px).
    /// </summary>
    /// <param name="row"></param>
    /// <param name="height"></param>
    public void SetHeight(int row, double height) => SetHeight(row, row, height);

    /// <summary>
    /// Sets the row headings from (and including) <paramref name="rowStart"/> to <paramref name="rowEnd"/> to <paramref name="heading"/>
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="heading"></param>
    public void SetHeadings(int rowStart, int rowEnd, string heading)
    {
        _sheet.Commands.ExecuteCommand(new SetRowHeadingsCommand(rowStart, rowEnd, heading));
    }
}

internal class RowInfoStoreRestoreData
{
    public List<(int start, int end, double height)> CumulativeHeightsModified { get; init; } = new();
    public List<(int start, int end, double height)> HeightsModified { get; init; } = new();
    public List<(int start, int end, string heading)> HeadingsModifed { get; init; } = new();
    public List<(int start, int end, bool visibile)> VisibilityModified { get; init; } = new();
    public RowColFormatRestoreData RowFormatRestoreData { get; init; } = new();

    public void Merge(RowInfoStoreRestoreData other)
    {
        CumulativeHeightsModified.AddRange(other.CumulativeHeightsModified);
        HeadingsModifed.AddRange(other.HeadingsModifed);
        VisibilityModified.AddRange(other.VisibilityModified);
        RowFormatRestoreData.Merge(other.RowFormatRestoreData);
    }
}