namespace BlazorDatasheet.Data;

public interface IFixedSizeRange : IRange, IEnumerable<CellPosition>
{
    public int Width { get; }
    public int Height { get; }
    public int Area { get; }
    public CellPosition End { get; }

    public CellPosition TopLeft => new CellPosition(
        Math.Min(Start.Row, End.Row),
        Math.Min(Start.Col, End.Col));
    
    public CellPosition BottomRight => new CellPosition(
        Math.Max(Start.Row, End.Row),
        Math.Max(Start.Col, End.Col));

    /// <summary>
    /// Break into a number of ranges that do not include the given range.
    /// </summary>
    /// <param name="range">The range to break around</param>
    /// <returns></returns>
    public List<IFixedSizeRange> Break(IFixedSizeRange range, bool preserveOrder = false);
    /// <summary>
    /// Break into a number of ranges that do not include the given position.
    /// </summary>
    /// <param name="position">The position to break around</param>
    /// <returns></returns>
    public List<IFixedSizeRange> Break(CellPosition position, bool preserveOrder = false);

    /// <summary>
    /// Returns an ordered copy of the range (Left-to-right, Up-to-down direction)
    /// </summary>
    /// <returns></returns>
    public IFixedSizeRange CopyOrdered();

    /// <summary>
    /// Change start/end positions so that the range is ordered in the direction specified
    /// </summary>
    /// <param name="rowDir"></param>
    /// <param name="colDir"></param>
    void SetOrder(int rowDir, int colDir);
}