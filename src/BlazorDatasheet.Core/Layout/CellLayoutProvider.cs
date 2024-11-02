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
        _sheet.Columns.GetVisualWidthBetween(Region.Left, Region.Right + 1) +
        (IncludeRowHeadings ? RowHeadingWidth : 0);

    /// <summary>
    /// The total height of the sheet including col headings
    /// </summary>
    public double TotalHeight =>
        _sheet.Rows.GetVisualHeightBetween(Region.Top, Region.Bottom + 1) + (IncludeColHeadings ? ColHeadingHeight : 0);

    public double RowHeadingWidth => _sheet.Rows.HeadingWidth;
    public double ColHeadingHeight => _sheet.Columns.HeadingHeight;

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
        return _sheet.Columns.GetVisualLeft(col) + extra;
    }

    public double ComputeRightPosition(int col)
    {
        return ComputeLeftPosition(col) + _sheet.Columns.GetVisualWidth(col);
    }

    public double ComputeRightPosition(IRegion region) => ComputeRightPosition(region.Right);
    public double ComputeBottomPosition(IRegion region) => ComputeBottomPosition(region.Bottom);

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
        return _sheet.Columns.GetColumnIndex(x - offset);
    }

    public int ComputeRow(double y)
    {
        var offset = IncludeColHeadings ? ColHeadingHeight : 0;
        return _sheet.Rows.GetRowIndex(y - offset);
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

    private IRegion Region => _sheet.Region;

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
        var totalWidth = _sheet.Columns.GetVisualWidthBetween(Region.Left, Region.Right + 1);
        var totalHeight = _sheet.Rows.GetVisualHeightBetween(Region.Top, Region.Bottom + 1);

        var regTopPosn = _sheet.Rows.GetVisualTop(Region.Top);
        var regLeftPosn = _sheet.Columns.GetColumnIndex(Region.Left);

        if (top > totalHeight - containerHeight)
            top = Math.Max(regTopPosn, totalHeight - containerHeight);
        if (left > totalWidth - containerWidth)
            left = Math.Max(regLeftPosn, totalWidth - containerWidth);

        Console.WriteLine($"Top: {top}, Left: {left}, Width: {totalWidth}, Height: {totalHeight}");

        // what would be seen on screen if there were no overflow
        var visibleRowStart = ComputeRow(top);
        var visibleColStart = ComputeColumn(left);
        visibleRowStart = Math.Max(visibleRowStart, 0);
        visibleColStart = Math.Max(visibleColStart, 0);

        var visibleRowEnd = ComputeRow(top + containerHeight);
        var visibleColEnd = ComputeColumn(left + containerWidth);

        visibleRowEnd = Math.Clamp(visibleRowEnd, Region.Top, Math.Max(Region.Bottom, 0));
        visibleColEnd = Math.Clamp(visibleColEnd, Region.Left, Math.Max(Region.Right, 0));

        var startRow = Math.Max(visibleRowStart - overflowY, Region.Top);
        var endRow = Math.Min(Math.Max(Region.Bottom, Region.Top), visibleRowEnd + overflowY);

        var startCol = Math.Max(visibleColStart - overflowX, Region.Left);
        var endCol = Math.Min(Math.Max(Region.Right, Region.Left), visibleColEnd + overflowX);

        var rowIndices = _sheet.Rows.GetVisibleIndices(startRow, endRow);
        var colIndices = _sheet.Columns.GetVisibleIndices(startCol, endCol);

        var region = new Region(
            rowIndices.FirstOrDefault(),
            rowIndices.LastOrDefault(),
            colIndices.FirstOrDefault(),
            colIndices.LastOrDefault());

        var leftPos = _sheet.Columns.GetVisualWidthBetween(Region.Left, startCol);
        var topPos = _sheet.Rows.GetVisualHeightBetween(Region.Top, startRow);
        var visibleWidth = ComputeWidthBetween(startCol, endCol + 1);
        var visibleHeight = ComputeHeightBetween(startRow, endRow + 1);
        var distRight = ComputeWidthBetween(endCol, Region.Right);
        var distBottom = ComputeHeightBetween(endRow, Region.Bottom);

        return new Viewport()
        {
            VisibleRegion = region,
            Left = leftPos,
            Top = topPos,
            DistanceBottom = distBottom,
            DistanceRight = distRight,
            VisibleWidth = visibleWidth,
            VisibleHeight = visibleHeight,
            NumberVisibleCols = colIndices.Count,
            NumberVisibleRows = rowIndices.Count,
            VisibleRowIndices = rowIndices,
            VisibleColIndices = colIndices
        };
    }
}