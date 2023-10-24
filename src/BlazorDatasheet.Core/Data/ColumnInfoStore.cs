using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public class ColumnInfoStore
{
    public double DefaultWidth { get; }
    private readonly Range1DStore<string> _headingStore = new(null);
    private readonly CumulativeRange1DStore _widthStore;

    public ColumnInfoStore(double defaultWidth)
    {
        DefaultWidth = defaultWidth;
        _widthStore = new CumulativeRange1DStore(defaultWidth);
    }

    /// <summary>
    /// Sets column width of one column and returns any column widths that were modified when set.
    /// </summary>
    /// <param name="col"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    internal List<(int start, int end, double width)> SetColumnWidth(int col, double width)
    {
        return _widthStore.Set(col, width);
    }

    /// <summary>
    /// Sets the column widths of all columns between (and including) the columns specified, to the value given.
    /// Returns any column ranges that were modified.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    internal List<(int start, int end, double width)> SetColumnWidths(int colStart, int colEnd, double width)
    {
        return _widthStore.Set(colStart, colEnd, width);
    }

    /// <summary>
    /// Sets the hae
    /// </summary>
    /// <param name="col"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal List<(int start, int end, string heading)> SetColumnHeading(int col, string heading)
    {
        return _headingStore.Set(col, heading);
    }

    /// <summary>
    /// Sets the headings of all columns between (and including) the columns specified, to the value given.
    /// Returns any column ranges that were modified.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal List<(int start, int end, string heading)> SetColumnHeadings(int colStart, int colEnd, string heading)
    {
        return _headingStore.Set(colStart, colEnd, heading);
    }

    /// <summary>
    /// Removes the columns between (and including) the indexes given.
    /// Handles shifting the column indices to the left and returns any modified data.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    internal ColumnInfoRestoreData Cut(int start, int end)
    {
        return new ColumnInfoRestoreData()
        {
            WidthsModified = _widthStore.Cut(start, end),
            HeadingsModifed = _headingStore.Cut(start, end)
        };
    }

    /// <summary>
    /// Inserts n empty columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    internal void Insert(int start, int n)
    {
        _widthStore.InsertAt(start, n);
        _headingStore.InsertAt(start, n);
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

    internal void RestoreFromData(ColumnInfoRestoreData data)
    {
        _widthStore.BatchSet(data.WidthsModified);
        _headingStore.BatchSet(data.HeadingsModifed);
    }
}

internal class ColumnInfoRestoreData
{
    public List<(int start, int end, double width)> WidthsModified { get; init; }
    public List<(int start, int end, string heading)> HeadingsModifed { get; init; }
}