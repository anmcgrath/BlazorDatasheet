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
        _sheet.Columns.GetVisualWidthBetween(0, _sheet.NumCols) + (IncludeRowHeadings ? RowHeadingWidth : 0);

    /// <summary>
    /// The total height of the sheet including col headings
    /// </summary>
    public double TotalHeight =>
        _sheet.Rows.GetVisualHeightBetween(0, _sheet.NumRows) + (IncludeColHeadings ? ColHeadingHeight : 0);

    public double RowHeadingWidth => _sheet.Columns.DefaultSize;
    public double ColHeadingHeight => _sheet.Rows.DefaultSize;

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
        return _sheet.Columns.GetVisualTop(col) + extra;
    }

    public double ComputeRightPosition(int col)
    {
        return ComputeLeftPosition(col) + _sheet.Columns.GetVisualWidth(col);
    }

    public double ComputeTopPosition(IRegion region)
    {
        return ComputeTopPosition(region.TopLeft.row);
    }

    public double ComputeTopPosition(int row)
    {
        var extra = IncludeColHeadings ? ColHeadingHeight : 0;
        return _sheet.Rows.GetVisualTop(row) + extra;
    }

    public double ComputeBottomPosition(int row)
    {
        return ComputeTopPosition(row) + _sheet.Rows.GetVisualHeight(row);
    }

    public double ComputeWidth(int startCol, int colSpan)
    {
        return _sheet.Columns.GetVisualWidthBetween(startCol, startCol + colSpan);
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
        var h = _sheet.Rows.GetVisualHeightBetween(startRow, startRow + rowSpan);
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
    /// <param name="containerHeight"> The height of the visual region</param>
    /// <param name="overflowX">The number of cells to include to the left/right of the "visible" viewport</param>
    /// <param name="overflowY">The number of cells to include to the top/bottom of the "visible" region.</param>
    /// <param name="containerWidth">The width of the visual region</param>
    /// <returns></returns>
    public Viewport GetViewPort(double left, double top, double containerWidth, double containerHeight, int overflowX,
        int overflowY)
    {
        // if top > total height of sheet we must have an issue...
        // if left > total width of sheet we must have an issue...
        // even if top > total height of sheet - container height we have an issue
        // even if left > total width of sheet - container width we have an issue
        var totalWidth = _sheet.Columns.GetVisualWidthBetween(0, _sheet.NumCols);
        var totalHeight = _sheet.Rows.GetVisualHeightBetween(0, _sheet.NumRows);
        if (top > totalHeight - containerHeight)
            top = Math.Max(0, totalHeight - containerHeight);
        if (left > totalWidth - containerWidth)
            left = Math.Max(0, totalWidth - containerWidth);

        // what would be seen on screen if there were no overflow
        var visibleRowStart = ComputeRow(top);
        var visibleColStart = ComputeColumn(left);
        visibleRowStart = Math.Max(visibleRowStart, 0);
        visibleColStart = Math.Max(visibleColStart, 0);

        var visibleRowEnd = ComputeRow(top + containerHeight);
        var visibleColEnd = ComputeColumn(left + containerWidth);

        visibleRowEnd = Math.Clamp(visibleRowEnd, 0, Math.Max(_sheet.NumRows - 1, 0));
        visibleColEnd = Math.Clamp(visibleColEnd, 0, Math.Max(_sheet.NumCols - 1, 0));

        var startRow = Math.Max(visibleRowStart - overflowY, 0);
        var endRow = Math.Min(Math.Max(_sheet.NumRows - 1, 0), visibleRowEnd + overflowY);

        var startCol = Math.Max(visibleColStart - overflowX, 0);
        var endCol = Math.Min(Math.Max(_sheet.NumCols - 1, 0), visibleColEnd + overflowX);

        var region = new Region(startRow, endRow, startCol, endCol);

        var leftPos = _sheet.Columns.GetVisualTop(startCol);
        var topPos = _sheet.Rows.GetVisualTop(startRow);
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
            VisibleHeight = visibleHeight,
            NumberVisibleCols = _sheet.Columns.CountVisible(startCol, endCol),
            NumberVisibleRows = _sheet.Rows.CountVisible(startRow, endRow)
        };
    }
}