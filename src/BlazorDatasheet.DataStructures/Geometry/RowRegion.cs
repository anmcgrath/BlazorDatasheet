using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Geometry;

/// <summary>
/// A row region. This region does not have an end row
/// </summary>
public class RowRegion : Region
{
    public RowRegion(int rowStart, int rowEnd) : base(rowStart, rowEnd, 0, int.MaxValue)
    {
    }

    public RowRegion(int row) : this(row, row)
    {
    }

    public override IRegion Copy()
    {
        return new RowRegion(TopLeft.row, BottomRight.row);
    }

    public override void ExtendTo(int row, int col, IRegion? regionLimit = null)
    {
        if (regionLimit == null)
        {
            End = new CellPosition(row, int.MaxValue);
            SetOrderedBounds();
            return;
        }

        var newRowEnd = SheetMath
            .ClampInt(regionLimit.TopLeft.row, regionLimit.BottomRight.row, row);

        End = new CellPosition(newRowEnd, int.MaxValue);
        SetOrderedBounds();
    }

    public override void Expand(Edge edges, int amount)
    {
        // Expand top and bottom sides bot not right or left
        if ((edges & Edge.Right) == Edge.Right)
            edges &= ~Edge.Right;
        if ((edges & Edge.Left) == Edge.Left)
            edges &= ~Edge.Left;
        base.Expand(edges, amount);
    }

    public override IRegion GetBoundingRegion(IRegion otherRegion)
    {
        return new RowRegion(Math.Min(otherRegion.TopLeft.row, this.TopLeft.row),
                             Math.Max(otherRegion.BottomRight.row, this.BottomRight.row));
    }

    public override void Shift(int dRow, int dCol)
    {
        base.Shift(dRow, 0);
    }

    public override IRegion Clone()
    {
        return new RowRegion(this.Start.row, this.End.row);
    }
}