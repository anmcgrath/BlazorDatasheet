using System.Collections;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A region that has a specific start/end position
/// </summary>
public class Region : IRegion
{
    public CellPosition TopLeft { get; private set; }
    public CellPosition BottomRight { get; private set; }
    public int Top => TopLeft.Row;
    public int Left => TopLeft.Col;
    public int Bottom => BottomRight.Row;
    public int Right => BottomRight.Col;

    /// <summary>
    /// Where the region was started
    /// </summary>
    public CellPosition Start { get; protected set; }

    /// <summary>
    /// Where the region ends
    /// </summary>
    public CellPosition End { get; protected set; }

    public int Height => Bottom >= int.MaxValue ? int.MaxValue : Bottom - Top + 1;
    public int Width => Right >= int.MaxValue ? int.MaxValue : Right - Left + 1;
    public int Area => Height * Width;


    /// <summary>
    /// A single (width/height = 1) region with position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public Region(int row, int col) : this(row, row, col, col)
    {
    }

    /// <summary>
    /// A rectangular region specified by start/end rows & cols
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    public Region(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        Start = new CellPosition(rowStart, colStart);
        End = new CellPosition(rowEnd, colEnd);
        SetOrderedBounds();
    }

    public Region(CellPosition start, CellPosition end)
    {
        Start = start;
        End = end;
        SetOrderedBounds();
    }

    protected void SetOrderedBounds()
    {
        var r0 = Math.Min(Start.Row, End.Row);
        var r1 = Math.Max(Start.Row, End.Row);
        var c0 = Math.Min(Start.Col, End.Col);
        var c1 = Math.Max(Start.Col, End.Col);

        TopLeft = new CellPosition(r0, c0);
        BottomRight = new CellPosition(r1, c1);
    }

    /// <summary>
    /// Determines whether a point is inside the region
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col)
    {
        return row >= TopLeft.Row &&
               row <= BottomRight.Row &&
               col >= TopLeft.Col &&
               col <= BottomRight.Col;
    }

    /// <summary>
    /// Determines whether a region is fully inside this region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public bool Contains(IRegion region)
    {
        return region.Start.Col >= Start.Col &&
               region.End.Col <= End.Col &&
               region.Start.Row >= Start.Row &&
               region.End.Row <= End.Row;
    }

    /// <summary>
    /// Determines whether the column is spanned by the region
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool SpansCol(int col)
    {
        return col >= TopLeft.Col && col <= BottomRight.Col;
    }

    /// <summary>
    /// Determines whether the row is spanned by the region
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool SpansRow(int row)
    {
        return row >= TopLeft.Row &&
               row <= BottomRight.Row;
    }

    public bool Spans(int index, Axis axis)
    {
        switch (axis)
        {
            case Axis.Col: return SpansCol(index);
            default: return SpansRow(index);
        }
    }

    public IRegion Collapse()
    {
        return new Region(TopLeft.Row, TopLeft.Col);
    }

    /// <summary>
    /// Returns the region (which will be one cell wide/high) that runs along the specified edge of this region
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public IRegion GetEdge(Edge edge)
    {
        switch (edge)
        {
            case Edge.Top:
                return new Region(this.Start.Row, this.Start.Row, this.Start.Col, this.End.Col);
            case Edge.Bottom:
                return new Region(this.End.Row, this.End.Row, this.Start.Col, this.End.Col);
            case Edge.Left:
                return new Region(this.Start.Row, this.End.Row, this.Start.Col, this.Start.Col);
            case Edge.Right:
                return new Region(this.Start.Row, this.End.Row, this.End.Col, this.End.Col);
        }

        return default;
    }

    /// <summary>
    /// Updates region so that it falls inside the region
    /// </summary>
    /// <param name="region"></param>
    public void Constrain(IRegion? region)
    {
        Constrain(region.TopLeft.Row, region.BottomRight.Row, region.TopLeft.Col, region.BottomRight.Col);
    }

    /// <summary>
    /// Updates the region so that i falls inside the limits
    /// </summary>
    /// <param name="otherRowStart"></param>
    /// <param name="otherRowEnd"></param>
    /// <param name="otherColStart"></param>
    /// <param name="otherColEnd"></param>
    public void Constrain(int otherRowStart, int otherRowEnd, int otherColStart, int otherColEnd)
    {
        var r0 = SheetMath.ClampInt(otherRowStart, otherRowEnd, this.TopLeft.Row);
        var r1 = SheetMath.ClampInt(otherRowStart, otherRowEnd, this.BottomRight.Row);
        var c0 = SheetMath.ClampInt(otherColStart, otherColEnd, this.TopLeft.Col);
        var c1 = SheetMath.ClampInt(otherColStart, otherColEnd, this.BottomRight.Col);
        this.Start = new CellPosition(r0, c0);
        this.End = new CellPosition(r1, c1);
        SetOrderedBounds();
    }

    /// <summary>
    /// Returns a new copy of the region that doesn't keep the order
    /// </summary>
    /// <returns></returns>
    public virtual IRegion Copy()
    {
        return new Region(TopLeft.Row, BottomRight.Row, TopLeft.Col, BottomRight.Col);
    }

    /// <summary>
    /// Returns a new copy of the region that does keep the order
    /// </summary>
    /// <returns></returns>
    public virtual IRegion Clone()
    {
        return new Region(this.Start.Row, this.End.Row, this.Start.Col, this.End.Col);
    }

    public int GetSize(Axis axis)
    {
        return axis == Axis.Col ? Width : Height;
    }

    public int GetLeadingEdgeOffset(Axis axis)
    {
        return axis == Axis.Col ? this.GetEdge(Edge.Left).Left : this.GetEdge(Edge.Top).Top;
    }

    public int GetTrailingEdgeOffset(Axis axis)
    {
        return axis == Axis.Col ? this.GetEdge(Edge.Right).Right : this.GetEdge(Edge.Bottom).Bottom;
    }

    public CellPosition GetConstrained(CellPosition cellPosition)
    {
        var r = cellPosition.Row;
        var c = cellPosition.Col;
        var r0 = GetLeadingEdgeOffset(Axis.Row);
        var r1 = GetTrailingEdgeOffset(Axis.Row);
        var c0 = GetLeadingEdgeOffset(Axis.Col);
        var c1 = GetTrailingEdgeOffset(Axis.Col);
        if (r < r0)
            r = r0;
        else if (r > r1)
            r = r1;
        if (c < c0)
            c = c0;
        else if (c > c1)
            c = c1;
        return new CellPosition(r, c);
    }

    /// <summary>
    /// Shift the entire region by the amount specified
    /// </summary>
    /// <param name="dRow"></param>
    /// <param name="dCol"></param>
    public virtual void Shift(int dRow, int dCol)
    {
        this.Start = new CellPosition(this.Start.Row + dRow, this.Start.Col + dCol);
        this.End = new CellPosition(this.End.Row + dRow, this.End.Col + dCol);
        this.SetOrderedBounds();
    }

    /// <summary>
    /// Grows the edges provided by the amount given
    /// </summary>
    /// <param name="edges"></param>
    /// <param name="amount"></param>
    public virtual void Expand(Edge edges, int amount)
    {
        var endCol = this.End.Col;
        var endRow = this.End.Row;
        var startCol = this.Start.Col;
        var startRow = this.Start.Row;
        if ((edges & Edge.Bottom) == Edge.Bottom)
        {
            // The order here is deliberate.
            // If endRow = startRow then we will push the endRow down so here we move the end row first if required
            // and the start row up. Similar logic for the right with the col.
            // The order reverses for left and top, where we prefer to move the start row first if required
            if (endRow == Bottom)
                endRow += amount;
            else if (startRow == Bottom)
                startRow += amount;
        }

        if ((edges & Edge.Right) == Edge.Right)
        {
            if (endCol == Right)
                endCol += amount;
            else if (startCol == Right)
                startCol += amount;
        }

        if ((edges & Edge.Top) == Edge.Top)
        {
            if (startRow == Top)
                startRow -= amount;
            else if (endRow == Top)
                endRow -= amount;
        }

        if ((edges & Edge.Left) == Edge.Left)
        {
            if (startCol == Left)
                startCol -= amount;
            else if (endCol == Left)
                endCol -= amount;
        }

        this.Start = new CellPosition(startRow, startCol);
        this.End = new CellPosition(endRow, endCol);
        this.SetOrderedBounds();
    }

    /// <summary>
    /// Shrinks the edges provided by the amount given
    /// </summary>
    /// <param name="edges"></param>
    /// <param name="amount"></param>
    public void Contract(Edge edges, int amount) => Expand(edges, -amount);

    /// <summary>
    /// Returns a new region that covers both this region and the region given
    /// </summary>
    /// <param name="otherRegion"></param>
    /// <returns></returns>
    public virtual IRegion GetBoundingRegion(IRegion otherRegion)
    {
        return new Region(
            Math.Min(this.TopLeft.Row, otherRegion.TopLeft.Row),
            Math.Max(this.BottomRight.Row, otherRegion.BottomRight.Row),
            Math.Min(this.TopLeft.Col, otherRegion.TopLeft.Col),
            Math.Max(this.BottomRight.Col, otherRegion.BottomRight.Col)
        );
    }

    public IRegion? GetIntersection(IRegion? region)
    {
        var x1 = this.BottomRight.Col;
        var x2 = region.BottomRight.Col;
        var y1 = this.BottomRight.Row;
        var y2 = region.BottomRight.Row;

        IRegion? intersection = null;

        var xL = Math.Max(this.TopLeft.Col, region.TopLeft.Col);
        var xR = Math.Min(x1, x2);
        if (xR < xL)
            intersection = null;
        else
        {
            var yB = Math.Min(y1, y2);
            var yT = Math.Max(this.TopLeft.Row, region.TopLeft.Row);
            if (yB < yT)
                intersection = null;
            else
                intersection = new Region(yT, yB, xL, xR);
        }

        return intersection;
    }

    public virtual void ExtendTo(int row, int col, IRegion? regionLimit = null)
    {
        this.End = new CellPosition(row, col);
        SetOrderedBounds();
        if (regionLimit != null)
            this.Constrain(regionLimit);
    }

    public List<IRegion> Break(CellPosition position)
    {
        return Break(new Region(position.Row, position.Col));
    }

    public List<IRegion> Break(IRegion inputRegion)
    {
        var region = inputRegion.GetIntersection(this);
        if (region == null)
            return new List<IRegion>() { this.Copy() };

        // The region can split into at most 4 regions
        // We attempt to create all the regions and don't use any that are empty
        // Start from top to bottom. For example, consider the below case where we
        // break the larger region by the cell shown as \\
        // This is a case where we end up with 4 regions shown as 1, 2, 3, 4

        // | 1  |  1 |  1 |
        // | 2  | \\ |  3 |
        // | 4  |  4 |  4 |

        // the case below shows ,for example, when we would end up with only region 3 and 4

        // | \\ | \\ |  3 |
        // | \\ | \\ |  3 |
        // | 4  |  4 |  4 |

        var r1IsEmpty = this.TopLeft.Row == region.TopLeft.Row;
        var r2IsEmpty = this.TopLeft.Col == region.TopLeft.Col;
        var r3IsEmpty = this.BottomRight.Col == region.BottomRight.Col;
        var r4IsEmpty = this.BottomRight.Row == region.BottomRight.Row;

        var regions = new List<IRegion>();

        if (!r1IsEmpty)
        {
            var r1 = new Region(this.TopLeft.Row, region.TopLeft.Row - 1,
                                this.TopLeft.Col,
                                this.BottomRight.Col);
            regions.Add(r1);
        }

        if (!r2IsEmpty)
        {
            var r2 = new Region(region.TopLeft.Row, region.BottomRight.Row, this.TopLeft.Col,
                                region.TopLeft.Col - 1);
            regions.Add(r2);
        }

        if (!r3IsEmpty)
        {
            var r3 = new Region(region.TopLeft.Row, region.BottomRight.Row, region.BottomRight.Col + 1,
                                this.BottomRight.Col);
            regions.Add(r3);
        }

        if (!r4IsEmpty)
        {
            var r4 = new Region(region.BottomRight.Row + 1, this.BottomRight.Row, this.TopLeft.Col,
                                this.BottomRight.Col);
            regions.Add(r4);
        }

        return regions;
    }

    [Obsolete]
    /// <summary>
    /// Returns a new copy of the region with the row, col starting point
    /// at the top left (minimum points).
    /// </summary>
    /// <returns></returns>
    public IRegion CopyOrdered()
    {
        return new Region(
            Math.Min(TopLeft.Row, BottomRight.Row),
            Math.Max(TopLeft.Row, BottomRight.Row),
            Math.Min(TopLeft.Col, BottomRight.Col),
            Math.Max(TopLeft.Col, BottomRight.Col)
        );
    }


    public IEnumerator<CellPosition> GetEnumerator()
    {
        var rowDir = BottomRight.Row >= TopLeft.Row ? 1 : -1;
        var colDir = BottomRight.Col >= TopLeft.Col ? 1 : -1;
        var row = TopLeft.Row;
        var col = TopLeft.Col;

        for (var i = 0; i < Height; i++)
        {
            // Reset column at start of each row
            col = TopLeft.Col;

            for (var j = 0; j < Width; j++)
            {
                yield return new CellPosition(row, col);
                col += colDir;
            }

            row += rowDir;
        }
    }

    public override string ToString()
    {
        return $"region from (r{TopLeft.Row}, c{TopLeft.Col}) to (r{BottomRight.Row}, c{BottomRight.Col})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is IRegion region)
            return region.TopLeft.Row == TopLeft.Row
                   && region.TopLeft.Col == TopLeft.Col
                   && region.BottomRight.Row == BottomRight.Row
                   && region.BottomRight.Col == BottomRight.Col;

        return false;
    }
}