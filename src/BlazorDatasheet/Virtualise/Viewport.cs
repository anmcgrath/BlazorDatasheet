using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Virtualise;

public class Viewport
{
    /// <summary>
    /// The rendered region (includes overscan row/cols)
    /// </summary>
    public Region ViewRegion { get; set; }
    /// <summary>
    /// The visible coordinates inside the scroll container (does not include overscan rows/cols)
    /// </summary>
    public Rect ViewRect { get; set; }

    public Viewport(Region viewRegion, Rect viewRect)
    {
        ViewRegion = viewRegion;
        ViewRect = viewRect;
    }
}