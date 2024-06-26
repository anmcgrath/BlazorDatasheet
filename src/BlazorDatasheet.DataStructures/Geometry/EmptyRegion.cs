namespace BlazorDatasheet.DataStructures.Geometry;

public class EmptyRegion : IRegion
{
    public bool Equals(IRegion? other)
    {
        return other is EmptyRegion;
    }

    public CellPosition Start => new(Top, Left);
    public CellPosition End => new(Bottom, Right);
    public CellPosition TopLeft => Start;
    public CellPosition BottomRight => End;
    public int Top => -1;
    public int Left => -1;
    public int Bottom => -1;
    public int Right => -1;
    public int Width => 0;
    public int Height => 0;
    public int Area => 0;
    public bool Contains(int row, int col) => false;

    public bool Contains(IRegion region) => false;

    public bool SpansCol(int col) => false;

    public bool SpansRow(int row) => false;

    public bool Spans(int index, Axis axis) => false;

    public IRegion Collapse() => new EmptyRegion();

    public IRegion Copy() => new EmptyRegion();

    public IRegion GetBoundingRegion(IRegion otherRegion) => new EmptyRegion();

    public IRegion? GetIntersection(IRegion? region) => null;

    public bool Intersects(IRegion? region) => false;

    public void ExtendTo(int row, int col, IRegion? regionLimit = null)
    {
    }

    public List<IRegion> Break(IRegion region) => [];

    public List<IRegion> Break(CellPosition position) => [];

    public List<IRegion> Break(IEnumerable<IRegion> regions) => [];

    public IRegion GetEdge(Edge edge) => new EmptyRegion();

    public IRegion Clone() => new EmptyRegion();

    public int GetSize(Axis axis) => 0;

    public int GetSize(Direction direction) => 0;

    public int GetLeadingEdgeOffset(Axis axis) => Left;

    public int GetTrailingEdgeOffset(Axis axis) => Right;

    public CellPosition GetConstrained(CellPosition cellPosition) => new CellPosition(Top, Left);

    public void Shift(int dRow, int dCol)
    {
    }

    public void Expand(Edge edges, int amount)
    {
    }

    public void Contract(Edge edges, int amount)
    {
    }

    public bool Contains(CellPosition position) => false;

    public void Shift(int dRowStart, int dColStart, int dRowEnd, int dColEnd)
    {
    }
}