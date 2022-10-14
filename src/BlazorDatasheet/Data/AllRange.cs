namespace BlazorDatasheet.Data;

/// <summary>
/// Range that applies to all cells
/// </summary>
public class AllRange : IRange
{
    public CellPosition StartPosition { get; }

    public AllRange()
    {
        StartPosition = new CellPosition(0, 0);
    }

    public bool Contains(int row, int col) => true;
    public bool SpansCol(int col) => true;

    public bool SpansRow(int row) => true;

    public IRange Collapse()
    {
        return new Range(0, 0);
    }

    public void Move(int dRow, int dCol, IFixedSizeRange rangeLimit = null)
    {
        // Can't move all range
    }

    public IRange Copy() => new AllRange();

    public IFixedSizeRange GetIntersection(IFixedSizeRange range)
    {
        // Since this range is all-encompassing, return a copy of the fixed range
        return range.Copy() as IFixedSizeRange;
    }

    public void ExtendTo(int row, int col, IFixedSizeRange rangeLimit = null)
    {
        // Cannot extend all-encompassing range
    }
}