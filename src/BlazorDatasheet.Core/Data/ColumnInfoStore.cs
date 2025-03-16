using BlazorDatasheet.Core.Data.Collections;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public class ColumnInfoStore : RowColInfoStore
{
    public ColumnFilterCollection Filters { get; }
    public NonEmptyColumnCollection NonEmpty { get; }

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
        Filters = new ColumnFilterCollection(sheet);
        NonEmpty = new NonEmptyColumnCollection(this);
    }

    /// <summary>
    /// Returns the column index at the position x
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public int GetColumnIndex(double x)
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
    /// Returns the physical width of the col. This is non-zero even if the col is
    /// hidden. For visual height, use <seealso cref="GetVisualWidth"/>
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public double GetPhysicalWidth(int col)
    {
        return SizeStore.Get(col);
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
    public double GetVisualLeft(int colIndex)
    {
        return CumulativeSizeStore.GetCumulative(colIndex);
    }
}