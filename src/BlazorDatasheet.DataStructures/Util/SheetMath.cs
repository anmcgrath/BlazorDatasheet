using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.DataStructures.Util;

public static class SheetMath
{
    public static Envelope ToEnvelope(this IRegion region)
    {
        var maxCol = (region.Right == int.MaxValue) ? int.MaxValue : region.Right + 1;
        var maxRow = (region.Bottom == int.MaxValue) ? int.MaxValue : region.Bottom + 1;
        return new Envelope(region.TopLeft.col,
                            region.TopLeft.row,
                            maxCol,
                            maxRow);
    }
}