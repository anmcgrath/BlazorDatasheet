using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Layout;

public interface IGridLayoutProvider
{
    /// <summary>
    /// The current view region of the layout provider.
    /// </summary>
    public IRegion ViewRegion { get; set; }

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
    /// The left position (start) of the column given, relative to the left edge of <see cref="ViewRegion"/>
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    double ComputeLeftPosition(int col);

    /// <summary>
    /// The right position (end) of the column given, relative to the left edge of <see cref="ViewRegion"/>. This is the column left + column width
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    double ComputeRightPosition(int col) => ComputeLeftPosition(col) + ComputeWidth(col, 1);

    /// <summary>
    /// The top position (start) of the row given, relative to the top edge of <see cref="ViewRegion"/>
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    double ComputeTopPosition(int row);

    /// <summary>
    /// The bottom position (end) of the row given, relative to the top edge of <see cref="ViewRegion"/>. This is the row start + row height
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
    /// Computes the view-relative column index at view-relative pixel position <paramref name="x"/>.
    /// Both input and output are relative to <see cref="ViewRegion"/>.<see cref="IRegion.Left"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    int ComputeColumn(double x);

    /// <summary>
    /// Computes the view-relative row index at view-relative pixel position <paramref name="y"/>.
    /// Both input and output are relative to <see cref="ViewRegion"/>.<see cref="IRegion.Top"/>
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    int ComputeRow(double y);

    /// <summary>
    /// Computes the height of <paramref name="rowSpan"/> rows, including <paramref name="startRow"/>
    /// </summary>
    /// <param name="startRow"></param>
    /// <param name="rowSpan"></param>
    /// <returns></returns>
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