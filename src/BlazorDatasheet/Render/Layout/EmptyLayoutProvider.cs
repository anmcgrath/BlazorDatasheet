using BlazorDatasheet.Core.Layout;

namespace BlazorDatasheet.Render.Layout;

public class EmptyLayoutProvider : IGridLayoutProvider
{
    public double TotalWidth => 0;
    public double TotalHeight => 0;
    public int NumRows => 0;
    public int NumColumns => 0;
    public double ComputeLeftPosition(int col) => 0;

    public double ComputeTopPosition(int row) => 0;

    public double ComputeWidth(int startCol, int colSpan) => 0;

    public int ComputeColumn(double x) => 0;

    public int ComputeRow(double y) => 0;

    public double ComputeHeight(int startRow, int rowSpan) => 0;

    public List<int> GetVisibleRowIndices(int startRow, int endRow) => new();

    public List<int> GetVisibleColumnIndices(int startColumn, int endColumn) => new();
}