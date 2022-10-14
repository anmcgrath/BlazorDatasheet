using BlazorDatasheet.Data;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Render;

/// <summary>
/// Provides useful functions for computing actual pixel widths of
/// cells in the sheet based on the range.
/// </summary>
public class CellLayoutProvider
{
    private bool ShowRowHeaders { get; }
    private Sheet _sheet;
    private readonly double _columnWidth;
    private readonly double _rowHeight;
    private readonly bool _showColHeaders;

    internal CellLayoutProvider(Sheet sheet, double columnWidth, double rowHeight, bool showColHeaders, bool showRowHeaders)
    {
        ShowRowHeaders = showRowHeaders;
        //TODO: Remove columnWidth and height in future to calculate dynamically
        //TODO: Remove bools for show headers and rows and determine from sheet.
        _sheet = sheet;
        _columnWidth = columnWidth;
        _rowHeight = rowHeight;
        _showColHeaders = showColHeaders;
    }

    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    public double ComputeLeftPosition(CellPosition cellPosition)
    {
        var extra = ShowRowHeaders ? 1 : 0;
        return (cellPosition.Col + extra) * _columnWidth;
    }

    public double ComputeTopPosition(CellPosition cellPosition)
    {
        var extra = _showColHeaders ? 1 : 0;
        return (cellPosition.Row + extra) * _rowHeight;
    }

    public double ComputeWidth(IFixedSizeRange range)
    {
        return range.Width * _columnWidth;
    }

    public double ComputeHeight(IFixedSizeRange range)
    {
        return range.Height * _rowHeight;
    }
}