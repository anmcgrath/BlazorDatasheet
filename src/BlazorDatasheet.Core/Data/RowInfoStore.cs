using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

public class RowInfoStore : RowColInfoStore
{
    private double _headingWidth = 60;

    public RowInfoStore(double defaultHeight, Sheet sheet) : base(defaultHeight, sheet, Axis.Row)
    {
    }

    /// <summary>
    /// The width (in px) of row headings.
    /// </summary>
    public double HeadingWidth
    {
        get => _headingWidth;
        set
        {
            _headingWidth = value;
            EmitSizeModified(-1, -1);
        }
    }

    /// <summary>
    /// Returns the row index at the position <paramref name="yPosition"/>
    /// </summary>
    /// <param name="yPosition"></param>
    /// <returns></returns>
    internal int GetRowIndex(double yPosition)
    {
        return CumulativeSizeStore.GetPosition(yPosition);
    }

    /// <summary>
    /// Returns the height of the row specified. This is the visual height, so it will be 0 if the row is hidden.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public double GetVisualHeight(int row)
    {
        return CumulativeSizeStore.GetSize(row);
    }

    /// <summary>
    /// Returns the physical height of the row. This is non-zero even if the row is
    /// hidden. For visual height, use <seealso cref="GetVisualHeight"/>
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public double GetPhysicalHeight(int row)
    {
        return SizeStore.Get(row);
    }

    /// <summary>
    /// Returns the distance between the top positions of two rows.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetVisualHeightBetween(int start, int end)
    {
        return CumulativeSizeStore.GetSizeBetween(start, end);
    }

    /// <summary>
    /// Returns the left position of the row index
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <returns></returns>
    public double GetVisualTop(int rowIndex)
    {
        return CumulativeSizeStore.GetCumulative(rowIndex);
    }
}