using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
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

    /// <summary>
    /// The total width of the sheet
    /// </summary>
    public double TotalWidth { get; private set; }

    /// <summary>
    /// The total height of the sheet
    /// </summary>
    public double TotalHeight { get; private set; }

    public CellLayoutProvider(Sheet sheet, double defaultColumnWidth, double defaultRowHeight)
    {
        _sheet = sheet;
        _sheet.ColumnInserted += SheetOnColumnInserted;
        _sheet.ColumnRemoved += SheetOnColumnRemoved;
        _sheet.RowInserted += SheetOnRowInserted;
        _sheet.RowRemoved += SheetOnRowRemoved;
        _defaultColumnWidth = defaultColumnWidth;
        _defaultRowHeight = defaultRowHeight;

        // Create default array of column widths
        _columnWidths = Enumerable.Repeat(defaultColumnWidth, sheet.NumCols).ToList();

        TotalWidth = defaultColumnWidth * sheet.NumCols;
        TotalHeight = defaultRowHeight * sheet.NumRows;
        updateXPositions();
    }

    private void SheetOnRowRemoved(object? sender, RowRemovedEventArgs e)
    {
        TotalHeight -= e.NRows * _defaultRowHeight;
    }

    private void SheetOnRowInserted(object? sender, RowInsertedEventArgs e)
    {
        TotalHeight += e.NRows * _defaultRowHeight;
    }

    private void SheetOnColumnRemoved(object? sender, ColumnRemovedEventArgs e)
    {
        for (int i = e.ColumnIndex; i < e.NCols; i++)
        {
            _columnWidths.RemoveAt(i);
        }

        updateXPositions();
    }

    private void SheetOnColumnInserted(object? sender, ColumnInsertedEventArgs e)
    {
        for (int i = e.ColumnIndex; i < e.NCols; i++)
        {
            _columnWidths.Insert(e.ColumnIndex, e.Width ?? _defaultColumnWidth);
        }

        updateXPositions();
    }

    public void SetColumnWidth(int col, double width)
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

        TotalWidth = cumX;
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

        var w = colXEnd - colXStart + _columnWidths[end];
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