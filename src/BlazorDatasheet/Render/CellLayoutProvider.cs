using BlazorDatasheet.Data;
using BlazorDatasheet.Events;

namespace BlazorDatasheet.Render;

/// <summary>
/// Provides useful functions for computing actual pixel widths of
/// cells in the sheet based on the region.
/// </summary>
public class CellLayoutProvider
{
    private Sheet _sheet;
    private readonly double _defaultColumnWidth;
    private readonly double _defaultRowHeight;

    // as long as the number of columns are small (which we will restrict) 
    // then we can store the column widths & x positions in arrays
    private List<double> _columnWidths { get; } = new();
    private List<double> _columnStartPositions { get; } = new();

    internal CellLayoutProvider(Sheet sheet, double defaultColumnWidth, double defaultRowHeight)
    {
        _sheet = sheet;
        _sheet.ColumnInserted += SheetOnColumnInserted;
        _defaultColumnWidth = defaultColumnWidth;
        _defaultRowHeight = defaultRowHeight;

        // Create default array of column widths
        _columnWidths = Enumerable.Repeat(defaultColumnWidth, sheet.NumCols).ToList();
        updateXPositions();
    }

    private void SheetOnColumnInserted(object? sender, ColumnInsertedEventArgs e)
    {
        _columnWidths.Insert(e.ColAfter + 1, _defaultColumnWidth);
        updateXPositions();
    }

    internal void SetColumnWidth(int col, double width)
    {
        _columnWidths[col] = width;
        updateXPositions();
    }

    private void updateXPositions()
    {
        _columnStartPositions.Clear();
        double cumX = 0;
        for (int i = 0; i < _columnWidths.Count; i++)
        {
            _columnStartPositions.Add(cumX);
            cumX += _columnWidths[i];
        }
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
        var extra = _sheet.ShowRowHeadings ? 1 : 0;
        if (col < 0)
            return 0;
        if (col > _columnWidths.Count - 1)
            return _columnWidths.Last() + _columnStartPositions.Last() +
                   ((col - _columnWidths.Count) + extra) * _defaultColumnWidth;
        return extra * _defaultColumnWidth + _columnStartPositions[col];
    }

    public double ComputeTopPosition(IRegion region)
    {
        return ComputeTopPosition(region.TopLeft.Row);
    }

    public double ComputeTopPosition(int row)
    {
        var extra = _sheet.ShowColumnHeadings ? 1 : 0;
        return (row + extra) * _defaultRowHeight;
    }

    public double ComputeWidth(int startCol, int colSpan)
    {
        if (startCol < 0 || startCol >= _columnWidths.Count)
            return _defaultColumnWidth;

        var end = Math.Min(startCol + colSpan - 1, _columnWidths.Count - 1);

        var colXStart = _columnStartPositions[startCol];
        var colXEnd = _columnStartPositions[end];

        var w = colXEnd - colXStart + _columnWidths[end] - 1;
        return w;
    }

    public double ComputeHeight(int rowSpan)
    {
        if (rowSpan > _sheet.NumRows)
            return (_sheet.NumRows * _defaultRowHeight);
        return rowSpan * _defaultRowHeight;
    }

    public double ComputeWidth(IRegion region)
    {
        return ComputeWidth(region.TopLeft.Col, region.Width);
    }

    public double ComputeHeight(IRegion region)
    {
        return ComputeHeight(region.Height);
    }
}