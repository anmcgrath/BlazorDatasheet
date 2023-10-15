using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Data;

public class RowInfoStore
{
    public double DefaultHeight { get; }
    private readonly Range1DStore<string> _headingStore = new(null);
    private readonly CumulativeRange1DStore _heightStore;

    public RowInfoStore(double defaultHeight)
    {
        DefaultHeight = defaultHeight;
        _heightStore = new CumulativeRange1DStore(defaultHeight);
    }

    /// <summary>
    /// Sets row height of one row and returns any row widths that were modified when set.
    /// </summary>
    /// <param name="col"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    internal List<(int start, int end, double width)> SetRowHeight(int col, double height)
    {
        return _heightStore.Set(col, height);
    }

    /// <summary>
    /// Sets the row heights of all columns between (and including) the rows specified, to the value given.
    /// Returns any row ranges that were modified.
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    internal List<(int start, int end, double width)> SetRowHeights(int rowStart, int rowEnd, double height)
    {
        return _heightStore.Set(rowStart, rowEnd, height);
    }

    /// <summary>
    /// Sets the heading for a row.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal List<(int start, int end, string heading)> SetRowHeading(int row, string heading)
    {
        return _headingStore.Set(row, heading);
    }

    /// <summary>
    /// Sets the headings of all columns between (and including) the columns specified, to the value given.
    /// Returns any column ranges that were modified.
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <param name="heading"></param>
    /// <returns></returns>
    internal List<(int start, int end, string heading)> SetRowHeadings(int colStart, int colEnd, string heading)
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
    internal RowInfoStoreRestoreData Cut(int start, int end)
    {
        return new RowInfoStoreRestoreData()
        {
            HeightsModified = _heightStore.Cut(start, end),
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
        _heightStore.InsertAt(start, n);
        _headingStore.InsertAt(start, n);
    }

    /// <summary>
    /// Returns the heading at the column index
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public string? GetHeading(int rowIndex)
    {
        return _headingStore.Get(rowIndex);
    }

    /// <summary>
    /// Returns the column index at the position x
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public int GetRow(double y)
    {
        return _heightStore.GetPosition(y);
    }

    /// <summary>
    /// Returns the width of the column specified.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public double GetHeight(int row)
    {
        return _heightStore.GetSize(row);
    }

    /// <summary>
    /// Returns the distance between the left positions of two columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetHeightBetween(int start, int end)
    {
        return _heightStore.GetSizeBetween(start, end);
    }

    /// <summary>
    /// Returns the left position of the column index
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns></returns>
    public double GetTop(int colIndex)
    {
        return _heightStore.GetCumulative(colIndex);
    }

    internal void RestoreFromData(RowInfoStoreRestoreData data)
    {
        _heightStore.BatchSet(data.HeightsModified);
        _headingStore.BatchSet(data.HeadingsModifed);
    }
}

internal class RowInfoStoreRestoreData
{
    public List<(int start, int end, double width)> HeightsModified { get; init; }
    public List<(int start, int end, string heading)> HeadingsModifed { get; init; }
}