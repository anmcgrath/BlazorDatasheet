using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Visual;

public class DirtySheetEventArgs
{
    public HashSet<(int row, int col)>? DirtyPositions { get; init; }
    public IEnumerable<IRegion>? DirtyRegions { get; init; }
}