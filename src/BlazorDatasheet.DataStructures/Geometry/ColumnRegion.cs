using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Geometry;

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
        return new ColumnRegion(TopLeft.col, BottomRight.col);
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
            .ClampInt(regionLimit.TopLeft.col, regionLimit.BottomRight.col, col);

        End = new CellPosition(int.MaxValue, newColumnEnd);
        SetOrderedBounds();
    }

    public override void Shift(int dRow, int dCol)
    {
        base.Shift(0, dCol);
    }

    public override void Expand(Edge edges, int amount)
    {
        // Expand left & right sides bot not top and bottom
        if ((edges & Edge.Bottom) == Edge.Bottom)
            edges &= ~Edge.Bottom;
        if ((edges & Edge.Top) == Edge.Top)
            edges &= ~Edge.Top;
        base.Expand(edges, amount);
    }

    public override IRegion GetBoundingRegion(IRegion otherRegion)
    {
        return new ColumnRegion(Math.Min(otherRegion.TopLeft.col, this.TopLeft.col),
                                Math.Max(otherRegion.BottomRight.col, this.BottomRight.col));
    }

    public override IRegion Clone()
    {
        return new ColumnRegion(this.Start.col, this.End.col);
    }
}