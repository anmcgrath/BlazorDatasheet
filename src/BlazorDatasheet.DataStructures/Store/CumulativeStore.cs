using System.Drawing;
using System.Numerics;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

public class CumulativeStore
{
    public readonly double Default;

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
    /// <param name="default">The size of a range if it has not been explicitly set.</param>
    public CumulativeStore(double @default)
    {
        Default = @default;
        _rangeIndices.Add(0);
        _rangeSizes.Add(@default);
        _cumulativeSizes.Add(0);
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
            _rangeSizes[rangeIndex] = size;
        else
        {
            findIndex = ~findIndex;
            _rangeIndices.Insert(findIndex, rangeIndex);
            _rangeSizes.Insert(findIndex, size);
            _cumulativeSizes.Insert(findIndex, 0);
        }

        CalculateCumulativeFromSearchIndex(findIndex);
    }

    private void CalculateCumulativeFromSearchIndex(int index)
    {
        var start = Math.Max(index - 1, 0);
        for (int i = start; i < _rangeIndices.Count; i++)
        {
            var rIndex = _rangeIndices[i];
            var rIndexPrev = i == 0 ? -1 : _rangeIndices[i - 1];
            var rSizePrev = i == 0 ? 0 : _rangeSizes[i - 1];
            var rCumPrev = i == 0 ? 0 : _cumulativeSizes[i - 1];
            var sizeDiff = (rIndex - rIndexPrev - 1) * Default + rSizePrev;
            _cumulativeSizes[i] = rCumPrev + sizeDiff;
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
            return Default;

        var closestRangeIndex = _rangeIndices.BinarySearchIndexOf(rangeIndex);
        if (closestRangeIndex < 0)
            return Default;
        if (closestRangeIndex > _rangeIndices.Count - 1)
            return Default;

        return _rangeSizes[closestRangeIndex];
    }

    /// <summary>
    /// Get the sizes between (and including) the range start/end.
    /// </summary>
    /// <param name="rangeStart"></param>
    /// <param name="rangeEnd"></param>
    /// <returns></returns>
    public IEnumerable<double> GetSizes(int rangeStart, int rangeEnd)
    {
        var sizes = new List<double>();
        for (int i = rangeStart; i < rangeEnd; i++)
            sizes.Add(GetSize(i));
        return sizes;
    }

    /// <summary>
    /// Returns the cumulative range size at the START of the range index.
    /// For the first range (0), it is always zero.
    /// </summary>
    /// <param name="rangeIndex"></param>
    /// <returns></returns>
    public double GetCumulative(int rangeIndex)
    {
        if (rangeIndex <= 0)
            return 0;

        if (!_rangeIndices.Any())
            return rangeIndex * Default;

        // cumulative at the range index given is
        var closestRangeIndex = _rangeIndices.BinarySearchIndexOf(rangeIndex);
        if (closestRangeIndex < 0)
            closestRangeIndex = Math.Max(0, ~closestRangeIndex);
        else
            return _cumulativeSizes[closestRangeIndex];

        //closestRangeIndex = index after we would have found the value, if it were in there

        if (closestRangeIndex > _rangeIndices.Count - 1)
        {
            var lastRangeIndex = _rangeIndices.Last();
            var lastSize = _rangeSizes.Last();
            var cum = _cumulativeSizes.Last() + (rangeIndex - lastRangeIndex - 1) * Default + lastSize;
            return cum;
        }

        return _cumulativeSizes[closestRangeIndex - 1] +
               _rangeSizes[closestRangeIndex - 1] * (rangeIndex - _rangeIndices[closestRangeIndex - 1]);
    }
    
    /// <summary>
    /// Returns the range position just BEFORE the cumulative position given.
    /// </summary>
    /// <param name="cumulative"></param>
    /// <returns></returns>
    public int GetPosition(double cumulative)
    {
        if (!_cumulativeSizes.Any() || _cumulativeSizes.Count == 1)
            return (int)(cumulative / Default);

        var searchIndex = _cumulativeSizes.BinarySearchIndexOf(cumulative);
        if (searchIndex >= 0)
            return _rangeIndices[searchIndex];

        searchIndex = ~searchIndex; // the next index after where it would have been found
        var diff = cumulative - _cumulativeSizes[searchIndex - 1];
        if (diff < _rangeSizes[searchIndex - 1]) // if it is between the rangeIndex cumulative pos + range size
            return _rangeIndices[searchIndex - 1];
        return (int)(_rangeIndices[searchIndex - 1] + 1 + ((diff - _rangeSizes[searchIndex - 1]) / Default));
    }

    public double GetSizeBetween(int rangeStart, int rangeEnd)
    {
        return GetCumulative(rangeEnd) - GetCumulative(rangeStart);
    }

    public void InsertAt(int rangeStart, double? width = null)
    {
        if (rangeStart < 0)
            return;

        width ??= Default;
        var searchIndex = _rangeIndices.BinarySearchClosest(rangeStart);
        for (int i = searchIndex; i < _rangeIndices.Count; i++)
            _rangeIndices[i]++;
        Set(searchIndex, width.Value);
    }

    /// <summary>
    /// Removes data at the range, shifts range indices accordingly and returns
    /// any ranges that were actually removed, if any.
    /// </summary>
    /// <param name="rangeStart"></param>
    public List<(int rangeIndex, double size)> Cut(int rangeStart, int n)
    {
        var removed = new List<(int, double)>();
        var searchIndex = Math.Max(0, _rangeIndices.BinarySearchClosest(rangeStart) - 1);
        int startIndexAbove = _rangeIndices.Count + 1;
        for (int i = searchIndex; i < _rangeIndices.Count; i++)
        {
            var r = _rangeIndices[i];
            if (r > rangeStart + n)
            {
                _rangeIndices[i]--;
                continue;
            }

            if (r < rangeStart)
                continue;

            startIndexAbove = Math.Min(startIndexAbove, i + 1);
            removed.Add((r, _rangeSizes[i]));
            _rangeIndices.RemoveAt(i);
            _rangeSizes.RemoveAt(i);
            _cumulativeSizes.RemoveAt(i);
            i--;
        }

        CalculateCumulativeFromSearchIndex(searchIndex);
        return removed;
    }
}