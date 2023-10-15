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
    public double TotalWidth => _sheet.ColumnWidths.GetCumulative(_sheet.NumCols);

    /// <summary>
    /// The total height of the sheet
    /// </summary>
    public double TotalHeight => _sheet.RowHeights.GetCumulative(_sheet.NumRows);

    public CellLayoutProvider(Sheet sheet)
    {
        _sheet = sheet;
    }

    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    public double ComputeLeftPosition(IRegion region, bool includeRowHeaders)
    {
        return ComputeLeftPosition(region.TopLeft.Col, includeRowHeaders);
    }

    public double ComputeLeftPosition(int col, bool includeRowHeaders)
    {
        var extra = includeRowHeaders ? 1 : 0;
        if (col < 0)
            return 0;

        return _sheet.ColumnWidths.GetCumulative(col) + extra;
    }

    public double ComputeTopPosition(IRegion region, bool includeRowHeaders)
    {
        return ComputeTopPosition(region.TopLeft.Row, includeRowHeaders);
    }

    public double ComputeTopPosition(int row, bool includeColHeaders)
    {
        var extra = includeColHeaders ? 1 : 0;
        if (row < 0)
            return 0;

        return _sheet.RowHeights.GetCumulative(row) + extra;
    }

    public double ComputeWidth(int startCol, int colSpan)
    {
        return _sheet.ColumnWidths.GetSizeBetween(startCol, startCol + colSpan);
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

    public int ComputeColumn(double x, bool includeRowHeaders)
    {
        var extra = includeRowHeaders ? -1 : 0;
        return _sheet.ColumnWidths.GetPosition(x) + extra;
    }

    public int ComputeRow(double y, bool includeColHeaders)
    {
        var extra = includeColHeaders ? -1 : 0;
        return _sheet.RowHeights.GetPosition(y) + extra;
    }

    public double ComputeHeight(int startRow, int rowSpan)
    {
        return _sheet.RowHeights.GetSizeBetween(startRow, startRow + rowSpan);
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