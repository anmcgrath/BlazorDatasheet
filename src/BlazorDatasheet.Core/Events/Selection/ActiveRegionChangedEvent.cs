using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Selection;

public class ActiveRegionChangedEvent
{
    public IRegion? OldRegion { get; }
    public IRegion? NewRegion { get; }

    public ActiveRegionChangedEvent(IRegion? oldRegion, IRegion? newRegion)
    {
        OldRegion = oldRegion;
        NewRegion = newRegion;
    }
}