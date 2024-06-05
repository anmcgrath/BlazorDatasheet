using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Layout;

/// <summary>
/// Provides useful functions for computing actual pixel widths of
/// cells in the sheet based on the region.
/// </summary>
public class CellLayoutProvider : IGridLayoutProvider
{
    private Sheet _sheet;

    /// <summary>
    /// The total width of the sheet including row headings
    /// </summary>
    public double TotalWidth =>
        _sheet.Columns.GetWidthBetween(0, _sheet.NumCols) + (IncludeRowHeadings ? RowHeadingWidth : 0);

    /// <summary>
    /// The total height of the sheet including col headings
    /// </summary>
    public double TotalHeight =>
        _sheet.Rows.GetHeightBetween(0, _sheet.NumRows) + (IncludeColHeadings ? ColHeadingHeight : 0);

    public double RowHeadingWidth => _sheet.Columns.DefaultWidth;
    public double ColHeadingHeight => _sheet.Rows.DefaultHeight;

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

    public CellPosition ComputeCell(double x, double y)
    {
        return new CellPosition(ComputeRow(y), ComputeColumn(x));
    }

    public Rect ComputeRect(CellPosition position)
    {
        var l = ComputeLeftPosition(position.col);
        var t = ComputeTopPosition(position.row);
        var w = ComputeWidth(position.col, 1);
        var h = ComputeHeight(position.row, 1);
        return new Rect(l, t, w, h);
    }

    public Rect ComputeRect(IRegion r)
    {
        var l = ComputeLeftPosition(r.Left);
        var t = ComputeTopPosition(r.Top);
        var w = ComputeWidth(r.Left, r.Width);
        var h = ComputeHeight(r.Top, r.Height);
        return new Rect(l, t, w, h);
    }

    public double ComputeLeftPosition(IRegion region)
    {
        return ComputeLeftPosition(region.TopLeft.col);
    }

    public double ComputeLeftPosition(int col)
    {
        var extra = IncludeRowHeadings ? RowHeadingWidth : 0;
        return _sheet.Columns.GetLeft(col) + extra;
    }

    public double ComputeRightPosition(int col)
    {
        return ComputeLeftPosition(col) + _sheet.Columns.GetWidth(col);
    }

    public double ComputeTopPosition(IRegion region)
    {
        return ComputeTopPosition(region.TopLeft.row);
    }

    public double ComputeTopPosition(int row)
    {
        var extra = IncludeColHeadings ? ColHeadingHeight : 0;
        return _sheet.Rows.GetTop(row) + extra;
    }

    public double ComputeBottomPosition(int row)
    {
        return ComputeTopPosition(row) + _sheet.Rows.GetHeight(row);
    }

    public double ComputeWidth(int startCol, int colSpan)
    {
        return _sheet.Columns.GetWidthBetween(startCol, startCol + colSpan);
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
        var offset = IncludeRowHeadings ? RowHeadingWidth : 0;
        return _sheet.Columns.GetColumn(x - offset);
    }

    public int ComputeRow(double y)
    {
        var offset = IncludeColHeadings ? ColHeadingHeight : 0;
        return _sheet.Rows.GetRow(y - offset);
    }


    public double ComputeHeight(int startRow, int rowSpan)
    {
        var h = _sheet.Rows.GetHeightBetween(startRow, startRow + rowSpan);
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
        var leftPos = _sheet.Columns.GetLeft(startCol);
        var topPos = _sheet.Rows.GetTop(startRow);
        var visibleWidth = ComputeWidthBetween(startCol, endCol + 1);
        var visibleHeight = ComputeHeightBetween(startRow, endRow + 1);
        var distRight = ComputeWidthBetween(endCol, _sheet.NumCols - 1);
        var distBottom = ComputeHeightBetween(endRow, _sheet.NumRows - 1);

        return new Viewport()
        {
            VisibleRegion = region,
            Left = leftPos,
            Top = topPos,
            DistanceBottom = distBottom,
            DistanceRight = distRight,
            VisibleWidth = visibleWidth,
            VisibleHeight = visibleHeight
        };
    }
}