namespace BlazorDatasheet.DataStructures.Geometry;

/// <summary>
/// Region that applies to all cells
/// </summary>
public class AllRegion : Region
{
    public AllRegion() : base(0, int.MaxValue, 0, int.MaxValue)
    {
    }
}