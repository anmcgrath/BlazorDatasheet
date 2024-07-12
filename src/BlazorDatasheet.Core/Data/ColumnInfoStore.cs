using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public class ColumnInfoStore : RowColInfoStore
{
    private Dictionary<int, IColumnFilter> _filters = new();

    private double _headingHeight = 24;

    /// <summary>
    /// The height (in px) of column headings
    /// </summary>
    public double HeadingHeight
    {
        get => _headingHeight;
        set
        {
            _headingHeight = value;
            EmitSizeModified(-1, -1);
        }
    }

    public ColumnInfoStore(double defaultHeight, Sheet sheet) : base(defaultHeight, sheet, Axis.Col)
    {
    }

    /// <summary>
    /// Returns the column index at the position x
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public int GetColumn(double x)
    {
        return CumulativeSizeStore.GetPosition(x);
    }

    /// <summary>
    /// Returns the visual width of the column specified.
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    public double GetVisualWidth(int column)
    {
        return CumulativeSizeStore.GetSize(column);
    }

    /// <summary>
    /// Returns the distance between the left positions of two columns.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetVisualWidthBetween(int start, int end)
    {
        return CumulativeSizeStore.GetSizeBetween(start, end);
    }

    /// <summary>
    /// Returns the left position of the column index
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns></returns>
    public double GetVisualTop(int colIndex)
    {
        return CumulativeSizeStore.GetCumulative(colIndex);
    }

    public void SetFilter(int column, IColumnFilter filter)
    {
        if (!_filters.TryAdd(column, filter))
            _filters[column] = filter;
    }

    public IColumnFilter? GetFilter(int column)
    {
        if (_filters.TryGetValue(column, out var columnFilter))
            return columnFilter;
        return null;
    }

    public void ApplyFilters()
    {
        Sheet.Commands.BeginCommandGroup();
        Sheet.Rows.Unhide(0, Sheet.NumRows);

        var handler = new FilterHandler();
        var hiddenIntervals = handler.GetHiddenRows(Sheet, _filters);

        foreach (var interval in hiddenIntervals)
        {
            Sheet.Rows.Hide(interval.Start, interval.End - interval.Start + 1);
        }

        Sheet.Commands.EndCommandGroup();
    }

    public void ClearFilters()
    {
        _filters.Clear();
    }
}