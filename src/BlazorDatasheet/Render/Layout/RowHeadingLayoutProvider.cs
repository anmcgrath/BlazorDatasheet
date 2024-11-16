using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Layout;

namespace BlazorDatasheet.Render.Layout;

public class RowHeadingLayoutProvider : IGridLayoutProvider
{
    private Sheet _sheet;

    public RowHeadingLayoutProvider(Sheet sheet)
    {
        _sheet = sheet;
    }

    public double TotalWidth => _sheet.Rows.HeadingWidth;
    public double TotalHeight => _sheet.Rows.GetVisualHeightBetween(0, NumRows);
    public int NumRows => _sheet.NumRows;
    public int NumColumns => 1;
    public double ComputeLeftPosition(int col) => 0;
    public double ComputeTopPosition(int row) => _sheet.Rows.GetVisualTop(row);
    public double ComputeWidth(int startCol, int colSpan) => _sheet.Rows.HeadingWidth;
    public int ComputeColumn(double x) => 0;
    public int ComputeRow(double y) => _sheet.Rows.GetRowIndex(y);

    public double ComputeHeight(int startRow, int rowSpan) =>
        _sheet.Rows.GetVisualHeightBetween(startRow, startRow + rowSpan);
    public List<int> GetVisibleRowIndices(int startRow, int endRow) => _sheet.Rows.GetVisibleIndices(startRow, endRow);
    public List<int> GetVisibleColumnIndices(int startColumn, int endColumn) => [0];
}