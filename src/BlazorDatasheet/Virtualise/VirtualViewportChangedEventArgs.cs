using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Virtualise;

public class VirtualViewportChangedEventArgs : EventArgs
{
    public Viewport Viewport { get; set; }
    public List<Region> NewRegions { get; set; }

    public VirtualViewportChangedEventArgs(Viewport viewport, List<Region> newRegions)
    {
        Viewport = viewport;
        NewRegions = newRegions;
    }
}