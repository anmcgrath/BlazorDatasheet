using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

public class SelectionChangedEventArgs
{
    public IReadOnlyList<IRegion> PreviousRegions { get; }
    public IReadOnlyList<IRegion> NewRegions { get; }
    
    public Sheet Sheet { get; }

    public SelectionChangedEventArgs(IReadOnlyList<IRegion> previousRegions, IReadOnlyList<IRegion> newRegions, Sheet sheet)
    {
        PreviousRegions = previousRegions;
        NewRegions = newRegions;
        Sheet = sheet;
    }
}