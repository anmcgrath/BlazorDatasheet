using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Layout;

namespace BlazorDatasheet.Render.Layout;

public class ColHeadingLayoutProvider : IGridLayoutProvider
{
    private Sheet _sheet;

    public ColHeadingLayoutProvider(Sheet sheet)
    {
        _sheet = sheet;
    }

    public double TotalWidth => _sheet.Columns.GetVisualWidthBetween(0, NumColumns);
    public double TotalHeight => _sheet.Columns.HeadingHeight;
    
    public int NumRows => 1;
    public int NumColumns => _sheet.NumCols;
    public double ComputeLeftPosition(int col) => _sheet.Columns.GetVisualLeft(col);
    public double ComputeTopPosition(int row) => 0;

    public double ComputeWidth(int startCol, int colSpan) =>
        _sheet.Columns.GetVisualWidthBetween(startCol, startCol + colSpan);

    public int ComputeColumn(double x) => _sheet.Columns.GetColumnIndex(x);

    public int ComputeRow(double y) => 0;

    public double ComputeHeight(int startRow, int rowSpan) => _sheet.Columns.HeadingHeight;

    public List<int> GetVisibleRowIndices(int startRow, int endRow) => [0];

    public List<int> GetVisibleColumnIndices(int startColumn, int endColumn) =>
        _sheet.Columns.GetVisibleIndices(startColumn, endColumn);
}