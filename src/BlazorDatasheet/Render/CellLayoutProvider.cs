using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

/// <summary>
/// Provides useful functions for computing actual pixel widths of
/// cells in the sheet based on the region.
/// </summary>
public class CellLayoutProvider
{
    private Sheet _sheet;

    /// <summary>
    /// The total width of the sheet
    /// </summary>
    public double TotalWidth => _sheet.ColumnInfo.GetWidthBetween(0, _sheet.NumCols);

    /// <summary>
    /// The total height of the sheet
    /// </summary>
    public double TotalHeight => _sheet.RowInfo.GetHeightBetween(0, _sheet.NumRows);

    public double RowHeadingWidth => _sheet.ColumnInfo.DefaultWidth;
    public double ColHeadingHeight => _sheet.RowInfo.DefaultHeight;

    public bool IncludeRowHeadings { get; set; }
    public bool IncludeColHeadings { get; set; }

    public CellLayoutProvider(Sheet sheet)
    {
        _sheet = sheet;
    }

    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    public double ComputeLeftPosition(IRegion region)
    {
        return ComputeLeftPosition(region.TopLeft.Col);
    }

    public double ComputeLeftPosition(int col)
    {
        var extra = IncludeRowHeadings ? RowHeadingWidth : 0;
        return _sheet.ColumnInfo.GetLeft(col) + extra;
    }

    public double ComputeTopPosition(IRegion region)
    {
        return ComputeTopPosition(region.TopLeft.Row);
    }

    public double ComputeTopPosition(int row)
    {
        var extra = IncludeColHeadings ? ColHeadingHeight : 0;
        return _sheet.RowInfo.GetTop(row) + extra;
    }

    public double ComputeWidth(int startCol, int colSpan)
    {
        return _sheet.ColumnInfo.GetWidthBetween(startCol, startCol + colSpan);
    }

    /// <summary>
    /// Computes the width between the start of start col, and the start of end col.
    /// </summary>
    /// <param name="startCol"></param>
    /// <param name="endCol"></param>
    /// <returns></returns>
    public double ComputeWidthBetween(int startCol, int endCol)
    {
        var span = (endCol - startCol);
        return ComputeWidth(startCol, span);
    }

    /// <summary>
    /// Computes the width between the start of start col, and the start of end col.
    /// </summary>
    /// <param name="startRow"></param>
    /// <param name="endRow"></param>
    /// <returns></returns>
    public double ComputeHeightBetween(int startRow, int endRow)
    {
        var span = (endRow - startRow);
        return ComputeHeight(startRow, span);
    }

    public int ComputeColumn(double x)
    {
        var offset = IncludeRowHeadings ? ColHeadingHeight : 0;
        return _sheet.ColumnInfo.GetColumn(x - offset);
    }

    public int ComputeRow(double y)
    {
        var offset = IncludeColHeadings ? RowHeadingWidth : 0;
        return _sheet.RowInfo.GetRow(y - offset);
    }

    public double ComputeHeight(int startRow, int rowSpan)
    {
        var h = _sheet.RowInfo.GetHeightBetween(startRow, startRow + rowSpan);
        return h;
    }

    public double ComputeWidth(IRegion region)
    {
        return ComputeWidth(region.Left, region.Width);
    }

    public double ComputeHeight(IRegion region)
    {
        return ComputeHeight(region.Top, region.Height);
    }

    /// <summary>
    /// Computes the viewport region that is visible based on the left/top position of the view.
    /// </summary>
    /// <param name="left">The left position of the visual region</param>
    /// <param name="top">The top position of the visual region</param>
    /// <param name="height"> The height of the visual region</param>
    /// <param name="overflowX">The number of cells to include to the left/right of the "visible" viewport</param>
    /// <param name="overflowY">The number of cells to include to the top/bottom of the "visible" region.</param>
    /// <param name="width">The width of the visual region</param>
    /// <returns></returns>
    public Viewport GetViewPort(double left, double top, double width, double height, int overflowX, int overflowY)
    {
        // what would be seen on screen if there were no overflow
        var visibleRowStart = ComputeRow(top);
        var visibleColStart = ComputeColumn(left);
        var visibleRowEnd = ComputeRow(top + height);
        var visibleColEnd = ComputeColumn(left + width);

        visibleRowEnd = Math.Min(_sheet.NumRows - 1, visibleRowEnd);
        visibleColEnd = Math.Min(_sheet.NumCols - 1, visibleColEnd);

        var startRow = Math.Max(visibleRowStart - overflowY, 0);
        var endRow = Math.Min(_sheet.NumRows - 1, visibleRowEnd + overflowY);

        var startCol = Math.Max(visibleColStart - overflowX, 0);
        var endCol = Math.Min(_sheet.NumCols - 1, visibleColEnd + overflowX);

        var region = new Region(startRow, endRow, startCol, endCol);
        var leftPos = _sheet.ColumnInfo.GetLeft(startCol);
        var topPos = _sheet.RowInfo.GetTop(startRow);
        var distRight = ComputeWidthBetween(endCol, _sheet.NumCols - 1);
        var distBottom = ComputeHeightBetween(endRow, _sheet.NumRows - 1);

        return new Viewport()
        {
            VisibleRegion = region,
            Left = leftPos,
            Top = topPos,
            DistanceBottom = distBottom,
            DistanceRight = distRight
        };
    }
}