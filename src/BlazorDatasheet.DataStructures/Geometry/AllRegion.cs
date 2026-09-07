using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet.DataStructures.Geometry;

/// <summary>
/// Region that applies to all cells
/// </summary>
public class AllRegion : Region
{
    public AllRegion() : base(0, int.MaxValue, 0, int.MaxValue)
    {
    }

    public override IRegion Clone() => new AllRegion();

    public override IRegion Copy() => new AllRegion();

    // Whole-sheet coverage is independent of changes to row and column indices.
    public override void Shift(int dRow, int dCol) { }

    public override void Shift(int dRowStart, int dRowEnd, int dColStart, int dColEnd) { }

    public override void Expand(Edge edges, int amount) { }
}
