namespace BlazorDatasheet.Util;

/// <summary>
/// The decimal text of small non-negative integers, shared so that row and column indices written
/// into the markup of every cell do not allocate a string (or box an int) per cell per render.
/// </summary>
internal static class IntText
{
    private static readonly string?[] Cache = new string?[1 << 16];

    public static string Get(int value)
    {
        if ((uint)value >= (uint)Cache.Length)
            return value.ToString();

        // a race here only produces two equal strings
        return Cache[value] ??= value.ToString();
    }
}
