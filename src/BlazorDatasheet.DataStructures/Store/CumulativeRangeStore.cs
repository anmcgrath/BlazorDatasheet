using System.Drawing;
using System.Numerics;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

public class CumulativeRangeStore
{
    private readonly double _defaultRangeSize;

    /// <summary>
    /// Any row indices that have custom sizes applied. This list is always INCREASING.
    /// </summary>
    private readonly List<int> _rangeIndices = new();

    /// <summary>
    /// The range sizes associated with the range indices
    /// </summary>
    private readonly List<double> _rangeSizes = new();

    /// <summary>
    /// The cumulative sizes associated with the range indices
    /// </summary>
    private readonly List<double> _cumulativeSizes = new();

    /// <summary>
    /// Stores/retrieves widths of ranges. Useful for setting column/row size, because
    /// we can calculate the x/y positions of a row/column as well as distances between rows/columns.
    /// Each range index has a particular size with the cumulative being the position at the START of the range
    /// E.g say we set the size of 0 to 20 and the size of 2 to 30
    /// We then have the following
    ///    0        1       2      3
    /// | 20 | | default | 30 | default |
    /// The cumulative of 0 is always zero, the cumulative of 1 is 20, the cumulative of 2 is 20 + default, for 3 = 20 + default + 30.
    /// </summary>
    /// <param name="defaultRangeSize">The size of a range if it has not been explicitly set.</param>
    public CumulativeRangeStore(double defaultRangeSize)
    {
        _defaultRangeSize = defaultRangeSize;
    }

    /// <summary>
    /// Sets the size of one range to a given value.
    /// </summary>
    /// <param name="rangeIndex"></param>
    /// <param name="size"></param>
    public void Set(int rangeIndex, double size)
    {
        var findIndex = _rangeIndices.BinarySearchIndexOf(rangeIndex);
        if (findIndex >= 0)
        {
            _rangeSizes[rangeIndex] = size;
            CalculateCumulative(findIndex);
            return;
        }

        findIndex = ~findIndex;
        _rangeIndices.Insert(findIndex, rangeIndex);
        _rangeSizes.Insert(findIndex, size);
        _cumulativeSizes.Insert(findIndex, -1);
        CalculateCumulative(findIndex);
    }


    /// <summary>
    /// Computes the cumulative sizes from a particular start index (not actual index, not range index).
    /// </summary>
    /// <param name="fromIndex"></param>
    private void CalculateCumulative(int startIndexInArray)
    {
        var start = startIndexInArray == 0 ? 0 : startIndexInArray - 1;
        if (start == 0)
        {
            _cumulativeSizes[0] = 0;
            return;
        }

        for (int i = start; i < _cumulativeSizes.Count; i++)
        {
            var rIndex = _rangeIndices[i];
            var rIndexPrev = _rangeIndices[i - 1];
            var sizeDiff = (rIndex - rIndexPrev) * _rangeSizes[rIndex];
            _cumulativeSizes[i] = _cumulativeSizes[i - 1] + sizeDiff;
        }
    }

    /// <summary>
    /// Returns the size of a particular index.
    /// </summary>
    /// <param name="rangeIndex"></param>
    /// <returns></returns>
    public double GetSize(int rangeIndex)
    {
        if (!_rangeIndices.Any())
            return _defaultRangeSize;

        var closestRangeIndex = _rangeIndices.BinarySearchIndexOf(rangeIndex);
        if (closestRangeIndex < 0)
            return _defaultRangeSize;
        if (closestRangeIndex > _rangeIndices.Count - 1)
            return _defaultRangeSize;

        return _rangeSizes[closestRangeIndex];
    }

    /// <summary>
    /// Returns the cumulative range size at the START of the range index.
    /// For the first range (0), it is always zero.
    /// </summary>
    /// <param name="rangeIndex"></param>
    /// <returns></returns>
    public double GetCumulative(int rangeIndex)
    {
        if (rangeIndex == 0)
            return 0;

        // cumulative at the range index given is
        // cumulative[closestRangeIndexBefore] + size[closestRangeIndexBefore] * (index - closetRangeIndexBefore[
        var closestRangeIndex = _rangeIndices.BinarySearchIndexOf(rangeIndex);
        if (closestRangeIndex < 0)
            closestRangeIndex = Math.Max(0, ~closestRangeIndex - 1);

        if (closestRangeIndex > _rangeIndices.Count - 1)
        {
            var lastRangeIndex = _rangeIndices.Last();
            var lastSize = _rangeSizes.Last();
            return _cumulativeSizes.Last() + (rangeIndex - lastRangeIndex) * lastSize;
        }

        return _cumulativeSizes[closestRangeIndex] +
               _rangeSizes[closestRangeIndex] * (rangeIndex - _rangeIndices[closestRangeIndex]);
    }

    public double GetSizeBetween(int rangeStart, int rangeEnd)
    {
        return GetCumulative(rangeEnd) - GetCumulative(rangeStart);
    }
}