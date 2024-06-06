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
    private readonly CumulativeRange1DStore _heightStore;
    internal readonly MergeableIntervalStore<CellFormat> RowFormats = new();

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
        _heightStore = new CumulativeRange1DStore(defaultHeight);
    }

    /// <summary>
    /// Sets the row heights of all rows between (and including) the rows specified, to the value given.
    /// Returns any row ranges that were modified.
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    internal List<(int start, int end, double width)> SetRowHeightsImpl(int rowStart, int rowEnd, double height)
    {
        var restoreData = _heightStore.Set(rowStart, rowEnd, height);
        RowHeightChanged?.Invoke(this, new RowHeightChangedEventArgs(rowStart, rowEnd, height));
        _sheet.MarkDirty(new RowRegion(rowStart, rowEnd));
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
            HeightsModified = _heightStore.Delete(start, end),
            HeadingsModifed = _headingStore.Delete(start, end),
            RowFormatRestoreData = new RowColFormatRestoreData()
            {
                IntervalsRemoved = RowFormats.Clear(start, end)
            }
        };

        RowFormats.ShiftLeft(start, (end - start) + 1);
        RowRemoved?.Invoke(this, new RowRemovedEventArgs(start, (end - start) + 1));
        _sheet.MarkDirty(new RowRegion(start, _sheet.NumRows));
        return res;
    }

    /// <summary>
    /// Inserts n empty rows.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    internal void InsertImpl(int start, int n)
    {
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
        return _heightStore.GetPosition(y);
    }

    /// <summary>
    /// Returns the height of the row specified.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public double GetHeight(int row)
    {
        return _heightStore.GetSize(row);
    }

    /// <summary>
    /// Returns the distance between the top positions of two rows.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetHeightBetween(int start, int end)
    {
        return _heightStore.GetSizeBetween(start, end);
    }

    /// <summary>
    /// Returns the left position of the row index
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public double GetTop(int rowIndex)
    {
        return _heightStore.GetCumulative(rowIndex);
    }

    internal void Restore(RowInfoStoreRestoreData data)
    {
        _heightStore.BatchSet(data.HeightsModified);
        _headingStore.BatchSet(data.HeadingsModifed);
        foreach (var added in data.RowFormatRestoreData.IntervalsAdded)
            RowFormats.Clear(added);
        RowFormats.AddRange(data.RowFormatRestoreData.IntervalsRemoved);

        foreach (var change in data.HeightsModified)
            RowHeightChanged?.Invoke(this, new RowHeightChangedEventArgs(change.start, change.end, change.height));
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
    public List<(int start, int end, double height)> HeightsModified { get; init; }
    public List<(int start, int end, string heading)> HeadingsModifed { get; init; }
    public RowColFormatRestoreData RowFormatRestoreData { get; init; }
}