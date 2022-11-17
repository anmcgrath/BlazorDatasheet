using BlazorDatasheet.Data;
using BlazorDatasheet.Data.SpatialDataStructures;

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
    
    /// <summary>
    /// Binary search for the index of a value. If the value is in the array, the index is returned,
    /// otherwise the returned index is the index of the next highest value.
    /// </summary>
    /// <param name="value">The value to search for</param>
    /// <param name="array">The array to search in</param>
    /// <returns></returns>
    public static int BinarySearchClosest<T>(this IList<T> list, T value) where T : IComparable
    {
        int index = list.BinarySearchIndexOf(value);
        if (index < 0)
            return ~index;
        return index;
    }
    
    public static Int32 BinarySearchIndexOf<T>(this IList<T> list, T value, IComparer<T> comparer = null)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        comparer = comparer ?? Comparer<T>.Default;

        Int32 lower = 0;
        Int32 upper = list.Count - 1;

        while (lower <= upper)
        {
            Int32 middle = lower + (upper - lower) / 2;
            Int32 comparisonResult = comparer.Compare(value, list[middle]);
            if (comparisonResult == 0)
                return middle;
            else if (comparisonResult < 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }

        return ~lower;
    }

    public static Envelope ToEnvelope(this IRegion region)
    {
        return new Envelope(region.TopLeft.Col, 
                                 region.TopLeft.Row, 
                                 region.BottomRight.Col,
                                 region.BottomRight.Row);
    }
}