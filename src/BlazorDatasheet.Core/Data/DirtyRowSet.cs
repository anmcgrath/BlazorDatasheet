using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// The rows marked dirty since the last re-render, kept as sorted, disjoint row intervals.
/// </summary>
/// <remarks>
/// Rows are marked one at a time for every recalculated formula and every undone cell, so adding
/// a row that is already covered, or that extends the last interval touched, must not allocate.
/// </remarks>
public sealed class DirtyRowSet
{
    private readonly List<Interval> _intervals = new();

    /// <summary>
    /// Whether any row is dirty.
    /// </summary>
    public bool Any() => _intervals.Count > 0;

    /// <summary>
    /// The dirty row intervals, sorted by start and never touching or overlapping.
    /// </summary>
    public IReadOnlyList<Interval> Intervals => _intervals;

    /// <summary>
    /// Returns a copy of the dirty row intervals.
    /// </summary>
    public List<Interval> GetAllIntervals() => new(_intervals);

    /// <summary>
    /// Marks every row between <paramref name="start"/> and <paramref name="end"/> (inclusive) dirty.
    /// </summary>
    public void Add(int start, int end)
    {
        if (end < start)
            return;

        // first interval whose end reaches start - 1, i.e. that overlaps or touches the new rows
        var first = FindFirstReaching((long)start - 1);
        if (first == _intervals.Count)
        {
            _intervals.Add(new Interval(start, end));
            return;
        }

        var existing = _intervals[first];
        if (existing.Start > (long)end + 1)
        {
            _intervals.Insert(first, new Interval(start, end));
            return;
        }

        // merge every interval that overlaps or touches [start, end] into one
        var mergedStart = Math.Min(start, existing.Start);
        var mergedEnd = Math.Max(end, existing.End);
        var last = first;
        while (last + 1 < _intervals.Count && _intervals[last + 1].Start <= (long)end + 1)
        {
            last++;
            mergedEnd = Math.Max(mergedEnd, _intervals[last].End);
        }

        if (last > first)
            _intervals.RemoveRange(first + 1, last - first);
        _intervals[first] = new Interval(mergedStart, mergedEnd);
    }

    /// <summary>
    /// Whether every row between <paramref name="start"/> and <paramref name="end"/> is dirty.
    /// </summary>
    public bool Covers(int start, int end)
    {
        var first = FindFirstReaching(start);
        return first < _intervals.Count && _intervals[first].Start <= start && _intervals[first].End >= end;
    }

    public void Clear() => _intervals.Clear();

    /// <summary>
    /// Index of the first interval whose end is at or after <paramref name="position"/>, or
    /// the count when there is none.
    /// </summary>
    private int FindFirstReaching(long position)
    {
        var count = _intervals.Count;
        // rows are usually marked in order, so check the last interval before searching
        if (count == 0 || _intervals[count - 1].End < position)
            return count;

        int lo = 0, hi = count - 1;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            if (_intervals[mid].End < position)
                lo = mid + 1;
            else
                hi = mid;
        }

        return lo;
    }
}
