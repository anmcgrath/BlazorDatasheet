using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render.Layout;

public class ColHeadingLayoutProvider : IGridLayoutProvider
{
    private Sheet _sheet;
    public IRegion ViewRegion { get; set; }

    public ColHeadingLayoutProvider(Sheet sheet)
    {
        _sheet = sheet;
        ViewRegion = sheet.Region;
    }

    public double TotalWidth => _sheet.Columns.GetVisualWidthBetween(ViewRegion.Left, ViewRegion.Right + 1);
    public double TotalHeight => _sheet.Columns.HeadingHeight;

    public int NumRows => 1;
    public int NumColumns => _sheet.NumCols;

    public double ComputeLeftPosition(int col) =>
        _sheet.Columns.GetVisualLeft(col) - _sheet.Columns.GetVisualLeft(ViewRegion.Left);

    public double ComputeTopPosition(int row) => 0;

    public double ComputeWidth(int startCol, int colSpan) =>
        _sheet.Columns.GetVisualWidthBetween(startCol, startCol + colSpan);

    public int ComputeColumn(double x)
    {
        return _sheet.Columns.GetColumnIndex(_sheet.Columns.GetVisualLeft(ViewRegion.Left) + x) - ViewRegion.Left;
    }

    public int ComputeRow(double y) => 0;

    public double ComputeHeight(int startRow, int rowSpan) => _sheet.Columns.HeadingHeight;

    public List<int> GetVisibleRowIndices(int startRow, int endRow) => [0];

    public List<int> GetVisibleColumnIndices(int startColumn, int endColumn) =>
        _sheet.Columns.GetVisibleIndices(startColumn, endColumn);
}