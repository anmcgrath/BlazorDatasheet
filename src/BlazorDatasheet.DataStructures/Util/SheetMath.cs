using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.DataStructures.Util;

public static class SheetMath
{
    public static Envelope ToEnvelope(this IRegion region)
    {
        return new Envelope(region.Left,
                            region.Top,
                            region.Right,
                            region.Bottom);
    }
}