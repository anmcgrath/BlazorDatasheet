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
        _sheet.Columns.GetVisualWidthBetween(ViewRegion.Left, ViewRegion.Right + 1);

    /// <summary>
    /// The total height of the sheet including col headings
    /// </summary>
    public double TotalHeight =>
        _sheet.Rows.GetVisualHeightBetween(ViewRegion.Top, ViewRegion.Bottom + 1);

    public int NumRows => _sheet.NumRows;
    public int NumColumns => _sheet.NumCols;

    private IRegion? _region = null;

    /// <summary>
    /// The view region that the datasheet is limited to.
    /// </summary>
    public IRegion ViewRegion
    {
        get => _region ?? _sheet.Region;
        set => _region = value;
    }


    public CellLayoutProvider(Sheet sheet)
    {
        _sheet = sheet;
    }

    public CellPosition ComputeCell(double x, double y)
    {
        return new CellPosition(ComputeRow(y), ComputeColumn(x));
    }

    public double ComputeLeftPosition(IRegion region)
    {
        return ComputeLeftPosition(region.TopLeft.col);
    }

    public double ComputeLeftPosition(int col)
    {
        return _sheet.Columns.GetVisualLeft(col) - _sheet.Columns.GetVisualLeft(ViewRegion.Left);
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
        return _sheet.Rows.GetVisualTop(row) - _sheet.Rows.GetVisualTop(ViewRegion.Top);
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
        return _sheet.Columns.GetColumnIndex(x) + ViewRegion.Left;
    }

    public int ComputeRow(double y)
    {
        return _sheet.Rows.GetRowIndex(y) + ViewRegion.Left;
    }


    public double ComputeHeight(int startRow, int rowSpan)
    {
        var h = _sheet.Rows.GetVisualHeightBetween(startRow, startRow + rowSpan);
        return h;
    }

    public List<int> GetVisibleRowIndices(int startRow, int endRow) => _sheet.Rows.GetVisibleIndices(startRow, endRow);

    public List<int> GetVisibleColumnIndices(int startColumn, int endColumn) =>
        _sheet.Columns.GetVisibleIndices(startColumn, endColumn);

    public double ComputeWidth(IRegion region)
    {
        return ComputeWidth(region.Left, region.Width);
    }

    public double ComputeHeight(IRegion region)
    {
        return ComputeHeight(region.Top, region.Height);
    }
}