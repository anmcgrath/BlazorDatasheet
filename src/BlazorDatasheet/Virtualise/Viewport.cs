using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Virtualise;

public class Viewport
{
    public Region ViewRegion { get; set; }
    public Rect ViewRect { get; set; }

    public Viewport(Region viewRegion, Rect viewRect)
    {
        ViewRegion = viewRegion;
        ViewRect = viewRect;
    }
}