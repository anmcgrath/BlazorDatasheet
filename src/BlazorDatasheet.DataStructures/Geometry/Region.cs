using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Geometry;

/// <summary>
/// A region that has a specific start/end position
/// </summary>
public class Region : IRegion
{
    public CellPosition TopLeft { get; private set; }
    public CellPosition BottomRight { get; private set; }
    public int Top => TopLeft.row;
    public int Left => TopLeft.col;
    public int Bottom => BottomRight.row;
    public int Right => BottomRight.col;

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

    public int Area
    {
        get
        {
            if (Height == int.MaxValue || Width == int.MaxValue)
                return int.MaxValue;

            return Height * Width;
        }
    }


    /// <summary>
    /// A single (width/height = 1) region with position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public Region(int row, int col) : this(row, row, col, col)
    {
    }

    public Region(CellPosition position) : this(position.row, position.col)
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
        var r0 = Math.Min(Start.row, End.row);
        var r1 = Math.Max(Start.row, End.row);
        var c0 = Math.Min(Start.col, End.col);
        var c1 = Math.Max(Start.col, End.col);

        TopLeft = new CellPosition(r0, c0);
        BottomRight = new CellPosition(r1, c1);
    }

    /// <summary>
    /// Determines whether a point is inside the region
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public virtual bool Contains(int row, int col)
    {
        return row >= TopLeft.row &&
               row <= BottomRight.row &&
               col >= TopLeft.col &&
               col <= BottomRight.col;
    }

    /// <summary>
    /// Determines whether a region is fully inside this region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public bool Contains(IRegion region)
    {
        return region.Left >= Left &&
               region.Right <= Right &&
               region.Top >= Top &&
               region.Bottom <= Bottom;
    }

    /// <summary>
    /// Determines whether the cell position is inside this region
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool Contains(CellPosition position)
    {
        return Contains(position.row, position.col);
    }

    /// <summary>
    /// Determines whether the column is spanned by the region
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool SpansCol(int col)
    {
        return col >= TopLeft.col && col <= BottomRight.col;
    }

    /// <summary>
    /// Determines whether the row is spanned by the region
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool SpansRow(int row)
    {
        return row >= TopLeft.row &&
               row <= BottomRight.row;
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
        return new Region(TopLeft.row, TopLeft.col);
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
                return new Region(this.Start.row, this.Start.row, this.Start.col, this.End.col);
            case Edge.Bottom:
                return new Region(this.End.row, this.End.row, this.Start.col, this.End.col);
            case Edge.Left:
                return new Region(this.Start.row, this.End.row, this.Start.col, this.Start.col);
            case Edge.Right:
                return new Region(this.Start.row, this.End.row, this.End.col, this.End.col);
        }

        return default;
    }

    /// <summary>
    /// Updates region so that it falls inside the region
    /// </summary>
    /// <param name="region"></param>
    public void Constrain(IRegion? region)
    {
        Constrain(region.TopLeft.row, region.BottomRight.row, region.TopLeft.col, region.BottomRight.col);
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
        var r0 = Math.Clamp(this.TopLeft.row, otherRowStart, otherRowEnd);
        var r1 = Math.Clamp(this.BottomRight.row, otherRowStart, otherRowEnd);
        var c0 = Math.Clamp(this.TopLeft.col, otherColStart, otherColEnd);
        var c1 = Math.Clamp(this.BottomRight.col, otherColStart, otherColEnd);
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
        return new Region(TopLeft.row, BottomRight.row, TopLeft.col, BottomRight.col);
    }

    /// <summary>
    /// Returns a new copy of the region that does keep the order
    /// </summary>
    /// <returns></returns>
    public virtual IRegion Clone()
    {
        return new Region(this.Start.row, this.End.row, this.Start.col, this.End.col);
    }

    public int GetSize(Axis axis)
    {
        return axis == Axis.Col ? Width : Height;
    }

    public int GetSize(Direction direction)
    {
        var axis = (direction == Direction.Down || direction == Direction.Up) ? Axis.Row : Axis.Col;
        return GetSize(axis);
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
        var r = cellPosition.row;
        var c = cellPosition.col;
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
        this.Start = new CellPosition(this.Start.row + dRow, this.Start.col + dCol);
        this.End = new CellPosition(this.End.row + dRow, this.End.col + dCol);
        this.SetOrderedBounds();
    }
    
    public virtual void Shift(int dRowStart, int dRowEnd, int dColStart, int dColEnd)
    {
        this.Start = new CellPosition(this.Start.row + dRowStart, this.Start.col + dColStart);
        this.End = new CellPosition(this.End.row + dRowEnd, this.End.col + dColEnd);
        this.SetOrderedBounds();
    }

    /// <summary>
    /// Grows the edges provided by the amount given
    /// </summary>
    /// <param name="edges"></param>
    /// <param name="amount"></param>
    public virtual void Expand(Edge edges, int amount)
    {
        var endCol = this.End.col;
        var endRow = this.End.row;
        var startCol = this.Start.col;
        var startRow = this.Start.row;
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
            Math.Min(this.TopLeft.row, otherRegion.TopLeft.row),
            Math.Max(this.BottomRight.row, otherRegion.BottomRight.row),
            Math.Min(this.TopLeft.col, otherRegion.TopLeft.col),
            Math.Max(this.BottomRight.col, otherRegion.BottomRight.col)
        );
    }

    public IRegion? GetIntersection(IRegion? region)
    {
        var x1 = this.BottomRight.col;
        var x2 = region.BottomRight.col;
        var y1 = this.BottomRight.row;
        var y2 = region.BottomRight.row;

        IRegion? intersection;

        var xL = Math.Max(this.TopLeft.col, region.TopLeft.col);
        var xR = Math.Min(x1, x2);
        if (xR < xL)
            intersection = null;
        else
        {
            var yB = Math.Min(y1, y2);
            var yT = Math.Max(this.TopLeft.row, region.TopLeft.row);
            if (yB < yT)
                intersection = null;
            else
                intersection = new Region(yT, yB, xL, xR);
        }

        return intersection;
    }

    public bool Intersects(IRegion? region) => GetIntersection(region) != null;

    public virtual void ExtendTo(int row, int col, IRegion? regionLimit = null)
    {
        this.End = new CellPosition(row, col);
        SetOrderedBounds();
        if (regionLimit != null)
            this.Constrain(regionLimit);
    }

    public List<IRegion> Break(CellPosition position)
    {
        return Break(new Region(position.row, position.col));
    }

    public List<IRegion> Break(IRegion inputRegion)
    {
        var region = inputRegion.GetIntersection(this);
        // If there's no intersection then there's no break and so 
        // we return this.
        if (region == null)
            return new List<IRegion>() { this };
        // If the region contains this then we break the whole thing and return
        // an empty list
        if (region.Contains(this))
            return new List<IRegion>();

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

        var r1IsEmpty = this.TopLeft.row == region.TopLeft.row;
        var r2IsEmpty = this.TopLeft.col == region.TopLeft.col;
        var r3IsEmpty = this.BottomRight.col == region.BottomRight.col;
        var r4IsEmpty = this.BottomRight.row == region.BottomRight.row;

        var regions = new List<IRegion>();

        if (!r1IsEmpty)
        {
            var r1 = new Region(this.TopLeft.row, region.TopLeft.row - 1,
                this.TopLeft.col,
                this.BottomRight.col);
            regions.Add(r1);
        }

        if (!r2IsEmpty)
        {
            var r2 = new Region(region.TopLeft.row, region.BottomRight.row, this.TopLeft.col,
                region.TopLeft.col - 1);
            regions.Add(r2);
        }

        if (!r3IsEmpty)
        {
            var r3 = new Region(region.TopLeft.row, region.BottomRight.row, region.BottomRight.col + 1,
                this.BottomRight.col);
            regions.Add(r3);
        }

        if (!r4IsEmpty)
        {
            var r4 = new Region(region.BottomRight.row + 1, this.BottomRight.row, this.TopLeft.col,
                this.BottomRight.col);
            regions.Add(r4);
        }

        return regions;
    }

    public List<IRegion> Break(IEnumerable<IRegion> regions)
    {
        var allBroken = new List<IRegion>() { this };
        var newBroken = new List<IRegion>();
        var toRemove = new List<IRegion>();

        foreach (var region in regions)
        {
            toRemove.Clear();
            newBroken.Clear();

            foreach (var broken in allBroken)
            {
                if (broken.GetIntersection(region) == null)
                    continue;
                
                toRemove.Add(broken);
                newBroken.AddRange(broken.Break(region));
            }

            foreach (var remove in toRemove)
                allBroken.Remove(remove);
            allBroken.AddRange(newBroken);
        }

        return allBroken;
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
            Math.Min(TopLeft.row, BottomRight.row),
            Math.Max(TopLeft.row, BottomRight.row),
            Math.Min(TopLeft.col, BottomRight.col),
            Math.Max(TopLeft.col, BottomRight.col)
        );
    }


    public IEnumerator<CellPosition> GetEnumerator()
    {
        var rowDir = BottomRight.row >= TopLeft.row ? 1 : -1;
        var colDir = BottomRight.col >= TopLeft.col ? 1 : -1;
        var row = TopLeft.row;

        for (var i = 0; i < Height; i++)
        {
            // Reset column at start of each row
            var col = TopLeft.col;

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
        return $"region from (r{TopLeft.row}, c{TopLeft.col}) to (r{BottomRight.row}, c{BottomRight.col})";
    }

    public bool Equals(IRegion? obj)
    {
        if (obj is IRegion region)
            return region.TopLeft.row == TopLeft.row
                   && region.TopLeft.col == TopLeft.col
                   && region.BottomRight.row == BottomRight.row
                   && region.BottomRight.col == BottomRight.col;

        return false;
    }
}