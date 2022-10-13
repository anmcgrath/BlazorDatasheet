namespace BlazorDatasheet.Data;

public interface IRange
{
    /// <summary>
    /// The start position (first cell position) in the range
    /// </summary>
    public CellPosition StartPosition { get; }
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
    public void Move(int dRow, int dCol);
    /// <summary>
    /// Returns a copy of the range
    /// </summary>
    /// <returns></returns>
    public IRange Copy();
    /// <summary>
    /// Returns an ordered copy of the range (Left-to-right, Up-to-down direction)
    /// </summary>
    /// <returns></returns>
    public IRange CopyOrdered();

}