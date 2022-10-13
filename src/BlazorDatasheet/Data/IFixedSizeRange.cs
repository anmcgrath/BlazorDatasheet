namespace BlazorDatasheet.Data;

public interface IFixedSizeRange : IRange, IEnumerable<CellPosition>
{
    public int Width { get; }
    public int Height { get; }
    public int Area { get; }
    public CellPosition StartPosition { get; }
    public CellPosition EndPosition { get; }

    /// <summary>
    /// Returns an ordered copy of the range (Left-to-right, Up-to-down direction)
    /// </summary>
    /// <returns></returns>
    public IFixedSizeRange CopyOrdered();
}