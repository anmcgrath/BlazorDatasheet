using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.Util;

public static class SheetMath
{
    /// <summary>
    /// Constrains a single value to be inside limit 1 and limit 2 (aka clamp)
    /// </summary>
    /// <param name="limit1"></param>
    /// <param name="limit2"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    public static int ClampInt(int limit1, int limit2, int val)
    {
        var min = Math.Min(limit1, limit2);
        var max = Math.Max(limit1, limit2);
        if (val < min)
            return min;
        if (val > max)
            return max;
        return val;
    }

    public static Envelope ToEnvelope(this IRegion region)
    {
        return new Envelope(region.TopLeft.Col, 
                                 region.TopLeft.Row, 
                                 region.BottomRight.Col,
                                 region.BottomRight.Row);
    }
}