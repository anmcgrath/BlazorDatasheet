using BlazorDatasheet.Data;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyRegion : IEnumerable<CellPosition>
{
    int RowStart { get; set; }
    int ColStart { get; set; }
    int Height { get; }
    int Width { get; }
    int Area { get; }

    /// <summary>
    /// Determines whether a point is inside the region
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    bool Contains(int row, int col);

    /// <summary>
    /// Determines whether the column is spanned by the range
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    bool SpansCol(int col);

    /// <summary>
    /// Determines whether the row is spanned by the range
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    bool SpansRow(int row);

    /// <summary>
    /// Updates the size of the range so that it is no larger than a range starting from (0, 0) with height/width =  (rows, cols)
    /// </summary>
    /// <param name="nRows"></param>
    /// <param name="nCols"></param>
    void Constrain(int nRows, int nCols);

    void Constrain(Region region);

    /// <summary>
    /// Returns a new copy of the range.
    /// </summary>
    /// <returns></returns>
    Region Copy();

    /// <summary>
    /// Returns a new copy of the range with the row, col starting point
    /// at the top left (minimum points).
    /// </summary>
    /// <returns></returns>
    Region CopyOrdered();
}