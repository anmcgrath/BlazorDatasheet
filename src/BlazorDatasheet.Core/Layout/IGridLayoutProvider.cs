namespace BlazorDatasheet.Core.Layout;

public interface IGridLayoutProvider
{
    /// <summary>
    /// The total width of the grid
    /// </summary>
    public double TotalWidth { get; }

    /// <summary>
    /// The total height of the grid
    /// </summary>
    public double TotalHeight { get; }

    /// <summary>
    /// The total number of rows in the grid
    /// </summary>
    public int NumRows { get; }

    /// <summary>
    /// The total number of columns in the grid.
    /// </summary>
    public int NumColumns { get; }

    /// <summary>
    /// The left position (start) of the column given
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    double ComputeLeftPosition(int col);

    /// <summary>
    /// The right position (end) of the column given. This is the column left + column width
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    double ComputeRightPosition(int col) => ComputeLeftPosition(col) + ComputeWidth(col, 1);

    /// <summary>
    /// The top position (start) of the row given.
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    double ComputeTopPosition(int row);

    /// <summary>
    /// The bottom position (end) of the row given. This is the row start + row height
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    double ComputeBottomPosition(int row) => ComputeTopPosition(row) + ComputeHeight(row, 1);

    /// <summary>
    /// Computes the width of <paramref name="colSpan"/> columns, including <paramref name="startCol"/>
    /// </summary>
    /// <param name="startCol"></param>
    /// <param name="colSpan"></param>
    /// <returns></returns>
    double ComputeWidth(int startCol, int colSpan);

    /// <summary>
    /// Computes the width between the start of <paramref name="startCol"/>, and the start of <paramref name="endCol"/>
    /// </summary>
    /// <param name="startCol"></param>
    /// <param name="endCol"></param>
    /// <returns></returns>
    double ComputeWidthBetween(int startCol, int endCol) => ComputeWidth(startCol, endCol - startCol);

    /// <summary>
    /// Computes the height between the start of <paramref name="startRow"/>, and the start of <paramref name="endRow"/>
    /// </summary>
    /// <param name="startRow"></param>
    /// <param name="endRow"></param>
    /// <returns></returns>
    double ComputeHeightBetween(int startRow, int endRow) => ComputeHeight(startRow, endRow - startRow);

    /// <summary>
    /// Computes the column at position <paramref name="x"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    int ComputeColumn(double x);

    /// <summary>
    /// Computes the row at position <paramref name="y"/>
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    int ComputeRow(double y);

    /// Computes the height of <paramref name="rowSpan"/> rows, including <paramref name="startRow"/>
    double ComputeHeight(int startRow, int rowSpan);

    /// <summary>
    /// Returns the visible row indices between (and including) <paramref name="startRow"/> and <paramref name="endRow"/>
    /// </summary>
    /// <param name="startRow"></param>
    /// <param name="endRow"></param>
    /// <returns></returns>
    List<int> GetVisibleRowIndices(int startRow, int endRow);

    /// <summary>
    ///Returns the visible column indices between (and including) <paramref name="startColumn"/> and <paramref name="endColumn"/>
    /// </summary>
    /// <param name="startColumn"></param>
    /// <param name="endColumn"></param>
    /// <returns></returns>
    List<int> GetVisibleColumnIndices(int startColumn, int endColumn);
}