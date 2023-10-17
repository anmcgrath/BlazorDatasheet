using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Events;
using BlazorDatasheet.Events.Layout;

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
}