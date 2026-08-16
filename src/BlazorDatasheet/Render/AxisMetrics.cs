using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Render;

/// <summary>
/// The size and visibility of a single row or column.
/// </summary>
/// <remarks>
/// Both values require a lookup into the sheet's interval stores, and both are the same for
/// every cell along the row or column. When a region scrolls into view these are resolved once
/// per row and per column and then reused across the cells of that region.
/// </remarks>
internal readonly struct AxisMetrics
{
    /// <summary>The visual height of the row, or visual width of the column, in px.</summary>
    public readonly double Size;

    /// <summary>Whether the row or column is visible (i.e. not hidden).</summary>
    public readonly bool IsVisible;

    public AxisMetrics(double size, bool isVisible)
    {
        Size = size;
        IsVisible = isVisible;
    }

    public static AxisMetrics ForRow(Sheet sheet, int row) =>
        new(sheet.Rows.GetVisualHeightBetween(row, row + 1), sheet.Rows.IsVisible(row));

    public static AxisMetrics ForColumn(Sheet sheet, int col) =>
        new(sheet.Columns.GetVisualWidthBetween(col, col + 1), sheet.Columns.IsVisible(col));
}
