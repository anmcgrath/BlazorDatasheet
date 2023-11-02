namespace BlazorDatasheet.DataStructures.Search;

public static class SearchExtensions
{
    /// <summary>
    /// Binary search for the index of a value. If the value is in the array, the index is returned,
    /// otherwise the returned index is the index of the next highest value.
    /// </summary>
    /// <param name="list">The array to search in</param>
    /// <param name="value">The value to search for</param>
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
}