using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A column region. This region does not have an end position.
/// </summary>
public class ColumnRegion : Region
{
    /// <summary>
    /// Create a column region that spans the start and end columns
    /// </summary>
    /// <param name="columnStart"></param>
    /// <param name="columnEnd"></param>
    public ColumnRegion(int columnStart, int columnEnd) : base(0, int.MaxValue, columnStart, columnEnd)
    {
    }

    public ColumnRegion(int column) : this(column, column)
    {
    }

    public override IRegion Copy()
    {
        return new ColumnRegion(TopLeft.Col, BottomRight.Col);
    }

    public override void ExtendTo(int row, int col, IRegion? regionLimit = null)
    {
        if (regionLimit == null)
        {
            End = new CellPosition(int.MaxValue, col);
            SetOrderedBounds();
            return;
        }

        var newColumnEnd = SheetMath
            .ClampInt(regionLimit.TopLeft.Col, regionLimit.BottomRight.Col, col);

        End = new CellPosition(int.MaxValue, newColumnEnd);
        SetOrderedBounds();
    }

    public override IRegion GetBoundingRegion(IRegion otherRegion)
    {
        return new ColumnRegion(Math.Min(otherRegion.TopLeft.Col, this.TopLeft.Col),
                                Math.Max(otherRegion.BottomRight.Col, this.BottomRight.Col));
    }

    public override IRegion Clone()
    {
        return new ColumnRegion(this.Start.Col, this.End.Col);
    }
}