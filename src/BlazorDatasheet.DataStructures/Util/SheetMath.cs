using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.DataStructures.Util;

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
    
    /// <summary>
    /// Constrains a single value to be inside limit 1 and limit 2 (aka clamp)
    /// </summary>
    /// <param name="limit1"></param>
    /// <param name="limit2"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    public static double ClampDouble(double limit1, double limit2, double val)
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
        var maxCol = (region.Right == int.MaxValue) ? int.MaxValue : region.Right + 1;
        var maxRow = (region.Bottom == int.MaxValue) ? int.MaxValue : region.Bottom + 1;
        return new Envelope(region.TopLeft.col,
                            region.TopLeft.row,
                            maxCol,
                            maxRow);
    }
}