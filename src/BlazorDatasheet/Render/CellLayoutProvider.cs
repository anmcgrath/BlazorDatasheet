using BlazorDatasheet.Data;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Render;

/// <summary>
/// Provides useful functions for computing actual pixel widths of
/// cells in the sheet based on the range.
/// </summary>
public class CellLayoutProvider
{
    private Sheet _sheet;
    private readonly double _columnWidth;
    private readonly double _rowHeight;

    internal CellLayoutProvider(Sheet sheet, double columnWidth, double rowHeight)
    {
        //TODO: Remove columnWidth and height in future to calculate dynamically
        //TODO: Remove bools for show headers and rows and determine from sheet.
        _sheet = sheet;
        _columnWidth = columnWidth;
        _rowHeight = rowHeight;
    }

    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    public double ComputeLeftPosition(IFixedSizeRange range)
    {
        return ComputeLeftPosition(range.TopLeft.Col);
    }

    public double ComputeLeftPosition(int col)
    {
        var extra = _sheet.ShowRowHeadings ? 1 : 0;
        return (col + extra) * _columnWidth;
    }

    public double ComputeTopPosition(IFixedSizeRange range)
    {
        return ComputeTopPosition(range.TopLeft.Row);
    }

    public double ComputeTopPosition(int row)
    {
        var extra = _sheet.ShowColumnHeadings ? 1 : 0;
        return (row + extra) * _rowHeight;
    }

    public double ComputeWidth(int colSpan)
    {
        return colSpan * _columnWidth - 1; // -1 accounts for borders
    }

    public double ComputeHeight(int rowSpan)
    {
        return rowSpan * _rowHeight - 1; // -1 accounts for borders
    }

    public double ComputeWidth(IFixedSizeRange range)
    {
        return ComputeWidth(range.Width);
    }

    public double ComputeHeight(IFixedSizeRange range)
    {
        return ComputeHeight(range.Height);
    }
}