using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public class ColumnInfoStore
{
    private readonly Sheet _sheet;
    public double DefaultWidth { get; }
    private readonly Range1DStore<string> _headingStore = new(null);
    private readonly CumulativeRange1DStore _widthStore;
    internal readonly MergeableIntervalStore<CellFormat> ColFormats = new();

    /// <summary>
    /// Fired when a column is inserted into the sheet
    /// </summary>
    public event EventHandler<ColumnInsertedEventArgs>? ColumnInserted;

    /// <summary>
    /// Fired when a column is removed from the sheet.
    /// </summary>
    public event EventHandler<ColumnRemovedEventArgs>? ColumnRemoved;

    /// <summary>
    /// Fired when a column width is changed
    /// </summary>
    public event EventHandler<ColumnWidthChangedEventArgs>? ColumnWidthChanged;


    public ColumnInfoStore(double defaultWidth, Sheet sheet)
    {
        _sheet = sheet;
        DefaultWidth = defaultWidth;
        _widthStore = new CumulativeRange1DStore(defaultWidth);
    }

    /// <summary>
    /// Sets the column widths of all columns between (and including) the columns specified, to the value given.
    /// Returns any column ranges that were modified.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    internal List<(int start, int end, double width)> SetColumnWidthsImpl(int colStart, int colEnd, double width)
    {
        var restoreData = _widthStore.Set(colStart, colEnd, width);
        ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(colStart, colEnd, width));
        _sheet.MarkDirty(new ColumnRegion(colStart, _sheet.NumCols));
        return restoreData;
    }

    /// <summary>
    /// Sets the headings of all columns between (and including) the columns specified, to the value given.
    /// Returns any column ranges that were modified.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal List<(int start, int end, string heading)> SetColumnHeadingsImpl(int colStart, int colEnd, string heading)
    {
        var restoreData = _headingStore.Set(colStart, colEnd, heading);
        _sheet.MarkDirty(new ColumnRegion(colStart, colEnd));
        return restoreData;
    }

    /// <summary>
    /// Removes the columns between (and including) the indexes given.
    /// Handles shifting the column indices to the left and returns any modified data.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    internal ColumnInfoRestoreData RemoveColumnsImpl(int start, int end)
    {
        var restoreData = new ColumnInfoRestoreData()
        {
            WidthsModified = _widthStore.Delete(start, end),
            HeadingsModifed = _headingStore.Delete(start, end),
            ColFormatRestoreData = new RowColFormatRestoreData()
            {
                IntervalsRemoved = ColFormats.Clear(start, end)
            }
        };

        ColFormats.ShiftLeft(start, (end - start) + 1);
        ColumnRemoved?.Invoke(this, new ColumnRemovedEventArgs(start, (end - start) + 1));
        _sheet.MarkDirty(new ColumnRegion(start, _sheet.NumCols));
        return restoreData;
    }

    /// <summary>
    /// Inserts a column after the index specified. If the index is outside of the range of -1 to NumCols-1,
    /// A column is added either at the beginning or end of the columns.
    /// </summary>
    /// <param name="colIndex"></param>
    /// <param name="nCols"></param>
    public void InsertAt(int colIndex, int nCols = 1)
    {
        var indexToAdd = Math.Min(_sheet.NumCols - 1, Math.Max(colIndex, 0));
        var cmd = new InsertColAtCommand(indexToAdd, nCols);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Inserts n empty columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    internal void InsertImpl(int start, int n)
    {
        _widthStore.InsertAt(start, n);
        _headingStore.InsertAt(start, n);
        ColFormats.ShiftRight(start, n);
        ColumnInserted?.Invoke(this, new ColumnInsertedEventArgs(start, n));
        _sheet.MarkDirty(new ColumnRegion(start, _sheet.NumCols));
    }

    /// <summary>
    /// Returns the heading at the column index
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    public string? GetHeading(int columnIndex)
    {
        return _headingStore.Get(columnIndex);
    }

    /// <summary>
    /// Returns the column index at the position x
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public int GetColumn(double x)
    {
        return _widthStore.GetPosition(x);
    }

    /// <summary>
    /// Returns the width of the column specified.
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    public double GetWidth(int column)
    {
        return _widthStore.GetSize(column);
    }

    /// <summary>
    /// Returns the distance between the left positions of two columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetWidthBetween(int start, int end)
    {
        return _widthStore.GetSizeBetween(start, end);
    }

    /// <summary>
    /// Returns the left position of the column index
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns></returns>
    public double GetLeft(int colIndex)
    {
        return _widthStore.GetCumulative(colIndex);
    }

    internal void Restore(ColumnInfoRestoreData data)
    {
        _widthStore.BatchSet(data.WidthsModified);
        _headingStore.BatchSet(data.HeadingsModifed);
        foreach (var added in data.ColFormatRestoreData.IntervalsAdded)
            ColFormats.Clear(added);
        ColFormats.AddRange(data.ColFormatRestoreData.IntervalsRemoved);

        foreach (var change in data.WidthsModified)
            ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(change.start, change.end, change.width));
    }

    public CellFormat? GetFormat(int column)
    {
        return ColFormats.Get(column);
    }

    internal RowColFormatRestoreData SetColumnFormatImpl(CellFormat cellFormat, ColumnRegion colRegion)
    {
        // Keep track of individual cell changes
        var cellChanges = new List<CellStoreRestoreData>();

        // we will ALWAYS merge the column regardless of what the cells are doing.
        var newOi = new OrderedInterval<CellFormat>(colRegion.Left, colRegion.Right, cellFormat.Clone());
        var modified = ColFormats.Add(newOi);

        // We need to find the merges between the new region and the row formats/cell formats and add those as cell formats.
        // this is because we the order of choosing the cell format is 1. cell format, then 2. col format then 3. row format.
        // if we set col format then a row format with some intersection, we would find that the col format is chosen when we
        // query the format at the intersection. It should be the cell format, so we set that.
        var rowOverlaps = _sheet.Rows.RowFormats.GetAllIntervals()
            .Select(x =>
                new DataRegion<CellFormat>(x.Data, new Region(x.Start, x.End, colRegion.Left, colRegion.Right)));

        var cellOverlaps = _sheet.Cells.GetFormatData(colRegion)
            .Select(x => new DataRegion<CellFormat>(x.Data, x.Region.GetIntersection(colRegion)!));

        // The intersectings region should be be merged with any existing (or empty) cell formats
        // So that the new, most recently applied format info is taken when the format is queried.
        // There may be some cell formats inside the col/row intersections in which case the format will be merged twice.
        // That should be ok because they will already exist and won't be added
        foreach (var overlap in rowOverlaps.Concat(cellOverlaps))
        {
            cellChanges.Add(_sheet.Cells.MergeFormatImpl(overlap.Region, cellFormat));
        }

        _sheet.MarkDirty(colRegion);

        return new RowColFormatRestoreData()
        {
            CellFormatRestoreData = cellChanges,
            IntervalsAdded = new List<OrderedInterval<CellFormat>>() { newOi },
            IntervalsRemoved = modified
        };
    }

    /// <summary>
    /// Removes the column at the specified index
    /// </summary>
    /// <param name="colIndex"></param>
    /// <param name="nCols">The number of oclumns to remove</param>
    /// <returns>Whether the column removal was successful</returns>
    public bool RemoveAt(int colIndex, int nCols = 1)
    {
        var cmd = new RemoveColumnCommand(colIndex, nCols);
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the width of a column, to the width given (in px).
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="width"></param>
    public void SetWidth(int colStart, int colEnd, double width)
    {
        var cmd = new SetColumnWidthCommand(colStart, colEnd, width);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the width of a column, to the width given (in px).
    /// </summary>
    /// <param name="column"></param>
    /// <param name="width"></param>
    /// <param name="colStart"></param>
    public void SetWidth(int column, double width)
    {
        SetWidth(column, column, width);
    }

    /// <summary>
    /// Sets the column headings from (and including) <paramref name="colStart"/> to <paramref name="colEnd"/> to <paramref name="heading"/>
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="heading"></param>
    public void SetHeadings(int colStart, int colEnd, string heading)
    {
        _sheet.Commands.ExecuteCommand(new SetColumnHeadingsCommand(colStart, colEnd, heading));
    }
}

internal class ColumnInfoRestoreData
{
    public List<(int start, int end, double width)> WidthsModified { get; init; }
    public List<(int start, int end, string heading)> HeadingsModifed { get; init; }

    public RowColFormatRestoreData ColFormatRestoreData { get; init; }
}