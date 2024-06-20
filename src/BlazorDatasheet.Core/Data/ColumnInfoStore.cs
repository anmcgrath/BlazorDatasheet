using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

public class ColumnInfoStore : RowColInfoStore
{
    
    public ColumnInfoStore(double defaultWidth, Sheet sheet) : base(defaultWidth, sheet, Axis.Col)
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
}