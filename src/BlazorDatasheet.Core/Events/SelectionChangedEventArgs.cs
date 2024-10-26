using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

public class SelectionChangedEventArgs
{
    public IReadOnlyList<IRegion> PreviousRegions { get; }
    public IReadOnlyList<IRegion> NewRegions { get; }

    public SelectionChangedEventArgs(IReadOnlyList<IRegion> previousRegions, IReadOnlyList<IRegion> newRegions)
    {
        PreviousRegions = previousRegions;
        NewRegions = newRegions;
    }
}