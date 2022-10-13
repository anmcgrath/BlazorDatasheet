namespace BlazorDatasheet.Util;

public static class SheetMath
{
    /// <summary>
    /// Constrains a single value to be inside max/min (aka clamp)
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    public static int ClampInt(int min, int max, int val)
    {
        if (val < min)
            return min;
        if (val > max)
            return max;
        return val;
    }
}