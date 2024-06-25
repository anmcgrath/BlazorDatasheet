namespace BlazorDatasheet.DataStructures.Geometry;

public interface IRegion : IEquatable<IRegion>
{
    /// <summary>
    /// Where the region was started
    /// </summary>
    public CellPosition Start { get; }

    /// <summary>
    /// Where the region ends
    /// </summary>
    public CellPosition End { get; }

    /// <summary>
    /// The top left (min) in the region
    /// </summary>
    public CellPosition TopLeft { get; }

    /// <summary>
    /// The bottom right (max) in the region
    /// </summary>
    public CellPosition BottomRight { get; }

    /// <summary>
    /// The first row in the region
    /// </summary>
    public int Top { get; }

    /// <summary>
    /// The first column in the region
    /// </summary>
    public int Left { get; }

    /// <summary>
    /// The last row in the region
    /// </summary>
    public int Bottom { get; }

    /// <summary>
    /// The last column in the region
    /// </summary>
    public int Right { get; }

    /// <summary>
    /// The width of the region, always greater than 0
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The height of the region, always greater than 0
    /// </summary>
    public int Height { get; }

    public int Area { get; }

    /// <summary>
    /// Determines whether a point is inside the region
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col);

    /// <summary>
    /// Determines whether a region is fully inside this region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public bool Contains(IRegion region);

    /// <summary>
    /// Determines whether the column is spanned by the region
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool SpansCol(int col);

    /// <summary>
    /// Determines whether the row is spanned by the region
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool SpansRow(int row);

    /// <summary>
    /// Determines whether the axis at position is spanned by region
    /// </summary>
    /// <param name="index"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public bool Spans(int index, Axis axis);

    /// <summary>
    /// Returns a new, collapsed region at the start position of this region.
    /// </summary>
    /// <returns></returns>
    public IRegion Collapse();

    /// <summary>
    /// Returns a copy of the region that doesn't keep the order
    /// </summary>
    /// <returns></returns>
    public IRegion Copy();

    /// <summary>
    /// Returns a new region that covers both this region and the region given
    /// </summary>
    /// <param name="otherRegion"></param>
    /// <returns></returns>
    public IRegion GetBoundingRegion(IRegion otherRegion);

    /// <summary>
    /// Returns a new region that is the intersection of this region and region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IRegion? GetIntersection(IRegion? region);

    /// <summary>
    /// Returns whether the other region intersects with this region at all.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public bool Intersects(IRegion? region);


    /// <summary>
    /// Extends this region TO the row and col specified
    /// </summary>
    /// <param name="row">The row to extend the region to</param>
    /// <param name="col">The column to extend the region to</param>
    /// <param name="regionLimit">The limiting region that the region cannot extend outside of</param>
    public void ExtendTo(int row, int col, IRegion? regionLimit = null);

    /// <summary>
    /// Break into a number of regions that do not include the given region.
    /// If there are no intersections, this region is returned.
    /// </summary>
    /// <param name="region">The region to break around</param>
    /// <returns></returns>
    public List<IRegion> Break(IRegion region);

    /// <summary>
    /// Break into a number of regions that do not include the given position.
    /// If there are no intersections, this region is returned.
    /// </summary>
    /// <param name="position">The position to break around</param>
    /// <returns></returns>
    public List<IRegion> Break(CellPosition position);

    /// <summary>
    /// Break into a number of regions that do not include the given region.
    /// If there are no intersections, this region is returned.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    List<IRegion> Break(IEnumerable<IRegion> regions);

    /// <summary>
    /// Returns the region (which will be one cell wide/high) that runs along the specified edge of this region
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public IRegion GetEdge(Edge edge);

    /// <summary>
    /// Returns a copy of the region that does keep the order
    /// </summary>
    /// <returns></returns>
    public IRegion Clone();

    /// <summary>
    /// Returns the width or height, depending on the axis given
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int GetSize(Axis axis);

    /// <summary>
    /// Returns the width or height, depending on the direction given
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public int GetSize(Direction direction);

    /// <summary>
    /// Returns the position of the leading edge.
    /// The leading edge is the first edge for the particular reading order
    /// The position is either row or col depending on the axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int GetLeadingEdgeOffset(Axis axis);

    /// <summary>
    /// Returns the position of the trailing edge.
    /// The trailing edge is the last edge for the particular reading order
    /// The position is either row or col depending on the axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int GetTrailingEdgeOffset(Axis axis);

    /// <summary>
    /// Returns a cell position that is constrained inside the region
    /// </summary>
    /// <param name="cellPosition"></param>
    /// <returns></returns>
    public CellPosition GetConstrained(CellPosition cellPosition);

    /// <summary>
    /// Shift the entire region by the amount specified
    /// </summary>
    /// <param name="dRow"></param>
    /// <param name="dCol"></param>
    public void Shift(int dRow, int dCol);

    /// <summary>
    /// Grows the edges provided by the amount given
    /// </summary>
    /// <param name="edges"></param>
    /// <param name="amount"></param>
    public void Expand(Edge edges, int amount);

    /// <summary>
    /// Shrinks the edges provided by the amount given
    /// </summary>
    /// <param name="edges"></param>
    /// <param name="amount"></param>
    public void Contract(Edge edges, int amount);

    /// <summary>
    /// Determines whether the cell position is inside this region
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    bool Contains(CellPosition position);

    bool IsSingleCell() => Height == 1 && Width == 1;

    /// <summary>
    /// Shift the entire region by the amount specified
    /// </summary>
    void Shift(int dRowStart, int dRowEnd, int dColStart, int dColEnd);
}