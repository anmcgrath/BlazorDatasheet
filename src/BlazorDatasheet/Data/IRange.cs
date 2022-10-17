namespace BlazorDatasheet.Data;

public interface IRange
{
    /// <summary>
    /// The start position (first cell position) in the range
    /// </summary>
    public CellPosition Start { get; }

    /// <summary>
    /// Determines whether a point is inside the range
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col);

    /// <summary>
    /// Determines whether the column is spanned by the range
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool SpansCol(int col);

    /// <summary>
    /// Determines whether the row is spanned by the range
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool SpansRow(int row);

    /// <summary>
    /// Returns a new, collapsed range at the start position of this range.
    /// </summary>
    /// <returns></returns>
    public IRange Collapse();

    /// <summary>
    /// Moves the entire range by the specified amount
    /// </summary>
    /// <param name="dRow"></param>
    /// <param name="dCol"></param>
    /// <param name="rangeLimit">The limiting range that the range cannot move outside of</param>
    public void Move(int dRow, int dCol, IFixedSizeRange? rangeLimit = null);

    /// <summary>
    /// Returns a copy of the range
    /// </summary>
    /// <returns></returns>
    public IRange Copy();

    /// <summary>
    /// Returns a new range that is the intersection of this range and range
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    public IFixedSizeRange GetIntersection(IFixedSizeRange? range);

    /// <summary>
    /// Extends this range TO the row and col specified
    /// </summary>
    /// <param name="row">The row to extend the range to</param>
    /// <param name="col">The column to extend the range to</param>
    /// <param name="rangeLimit">The limiting range that the range cannot extend outside of</param>
    public void ExtendTo(int row, int col, IFixedSizeRange? rangeLimit = null);
}