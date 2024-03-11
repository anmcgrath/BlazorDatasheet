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
    double ComputeRightPosition(int col);

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
    double ComputeBottomPosition(int row);

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
    double ComputeWidthBetween(int startCol, int endCol);

    /// <summary>
    /// Computes the height between the start of <paramref name="startRow"/>, and the start of <paramref name="endRow"/>
    /// </summary>
    /// <param name="startRow"></param>
    /// <param name="endRow"></param>
    /// <returns></returns>
    double ComputeHeightBetween(int startRow, int endRow);

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
}