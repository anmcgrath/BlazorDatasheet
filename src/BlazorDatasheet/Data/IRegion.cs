using BlazorDatasheet.Data.SpatialDataStructures;

namespace BlazorDatasheet.Data;

public interface IRegion
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
    /// Extends this region TO the row and col specified
    /// </summary>
    /// <param name="row">The row to extend the region to</param>
    /// <param name="col">The column to extend the region to</param>
    /// <param name="regionLimit">The limiting region that the region cannot extend outside of</param>
    public void ExtendTo(int row, int col, IRegion? regionLimit = null);

    /// <summary>
    /// Break into a number of regions that do not include the given region.
    /// </summary>
    /// <param name="region">The region to break around</param>
    /// <returns></returns>
    public List<IRegion> Break(IRegion region);

    /// <summary>
    /// Break into a number of regions that do not include the given position.
    /// </summary>
    /// <param name="position">The position to break around</param>
    /// <returns></returns>
    public List<IRegion> Break(CellPosition position);

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
    /// Returns the position of the leading edge.
    /// The leading edge is the first edge for the particular reading order
    /// The position is either row or col depending on the axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int GetLeadingEdgeOffset(Axis axis);

    /// <summary>
    /// Returns the position of the trailing edge.
    /// The trailing edge is the first edge for the particular reading order
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
}