using System.Diagnostics;
using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Intervals;

/// <summary>
/// A set of non-overlapping intervals containing some mergeable data.
/// When a new interval is added, the data from that interval is merged into any existing.
/// </summary>
/// <typeparam name="T"></typeparam>
public class MergeableIntervalStore<T> where T : IMergeable<T>
{
    /// <summary>
    ///  The minimum value of all ranges
    /// </summary>
    public int Start { get; private set; } = Int32.MaxValue;

    /// <summary>
    /// The maximum value of all ranges
    /// </summary>
    public int End { get; private set; } = Int32.MinValue;

    /// <summary>
    /// The sorted list of intervals, which we maintain to not be overlapping.
    /// The intervals are sorted by their start but could just as easily be sorted
    /// by their end position.
    /// </summary>
    private SortedList<int, OrderedInterval<T>> _Intervals { get; } = new();

    /// <summary>
    /// The default value returned if there is no value found.
    /// </summary>
    public T? DefaultValue { get; }

    public MergeableIntervalStore(T? defaultValue = default(T))
    {
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Whether we have any intervals stored.
    /// </summary>
    /// <returns></returns>
    public bool Any() => _Intervals.Any();

    /// <summary>
    /// Returns the data (if any) associated with the interval containing the position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public T? Get(int position)
    {
        if (!_Intervals.Any())
            return DefaultValue;

        if (position < Start || position > End)
            return DefaultValue;

        var i0 = _Intervals.Keys.BinarySearchIndexOf(position);
        if (i0 < 0)
            i0 = ~i0; // closest value to it (with start index greater than it)
        else
            return _Intervals[_Intervals.Keys[i0]].Data;

        // Now we have the next closest with a start index greater position
        // but it can't be that interval because position < start.
        // so we check the one before i0 to see if position is contained
        if (i0 - 1 < 0)
            return DefaultValue;

        if (_Intervals[_Intervals.Keys[i0 - 1]].Contains(position))
            return _Intervals[_Intervals.Keys[i0 - 1]].Data;

        return DefaultValue;
    }

    public List<OrderedInterval<T>> Add(int start, int end, T value)
    {
        return Add(new OrderedInterval<T>(start, end, value));
    }

    /// <summary>
    /// Adds the interval and merges any overlapping data into it.
    /// </summary>
    /// <param name="interval"></param>
    /// <returns>Intervals that were either modified or removed while adding</returns>
    public List<OrderedInterval<T>> Add(OrderedInterval<T> interval)
    {
        Start = Math.Min(interval.Start, Start);
        End = Math.Max(interval.End, End);

        var overlapping = GetIntervals(interval);
        if (!overlapping.Any())
        {
            _Intervals.Add(interval.Start, interval);
            UpdateStartEndPositions();
            return new List<OrderedInterval<T>>()
            {
                new OrderedInterval<T>(interval.Start, interval.End, DefaultValue)
            };
        }

        // Handle when interval extends before the first overlapping interval
        if (interval.Start < overlapping.First().Start)
            _Intervals.Add(interval.Start,
                new OrderedInterval<T>(interval.Start, overlapping.First().Start - 1, interval.Data));
        // Handle when interval extends after the last overlapping interval
        if (interval.End > overlapping.Last().End)
            _Intervals.Add(overlapping.Last().End + 1,
                new OrderedInterval<T>(overlapping.Last().End + 1, interval.End, interval.Data));

        var modified = new List<OrderedInterval<T>>();

        for (int i = 0; i < overlapping.Count; i++)
        {
            var oi = overlapping[i];
            if (interval.Contains(oi))
            {
                // We will have modified/removed the original data, so store it so we can keep a record (for undo)
                // in this case it is the entire interval because it was contained inside the added interval.
                modified.Add(new OrderedInterval<T>(oi.Start, oi.End, oi.Data.Clone()));
                oi.Data = oi.Data.Clone();
                oi.Data.Merge(interval.Data);
            }

            else if (oi.Contains(interval))
            {
                // We have [o, o, i, i, o, o] where o = overlapping interval
                // and i = interval we are adding
                // we remove o and add o0, and o1 so that we now have
                // [o0, o0, i, i, o1, o1]

                // first store the (removed) original data that was in o.
                modified.Add(new OrderedInterval<T>(interval.Start, interval.End, oi.Data.Clone()));

                _Intervals.Remove(oi.Start);
                if (oi.Start != interval.Start)
                    _Intervals.Add(oi.Start, new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data));
                var merged = new OrderedInterval<T>(interval.Start, interval.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);
                _Intervals.Add(merged.Start, merged);
                if (oi.End != interval.End)
                    _Intervals.Add(interval.End + 1, new OrderedInterval<T>(interval.End + 1, oi.End, oi.Data));
            }

            else if (interval.Start > oi.Start)
            {
                // [o, o, i, i] i, i
                // first store the (removed) original data from o
                modified.Add(new OrderedInterval<T>(interval.Start, oi.End, oi.Data.Clone()));

                _Intervals.Remove(oi.Start);
                var old = new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data);
                var merged = new OrderedInterval<T>(interval.Start, oi.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);
                _Intervals.Add(old.Start, old);
                _Intervals.Add(merged.Start, merged);
            }
            else if (interval.End < oi.End)
            {
                // i, i [i, i, o, o] 
                // first store the (removed) original data from o
                modified.Add(new OrderedInterval<T>(oi.Start, interval.End, oi.Data.Clone()));

                _Intervals.Remove(oi.Start);
                var old = new OrderedInterval<T>(interval.End + 1, oi.End, oi.Data);
                var merged = new OrderedInterval<T>(oi.Start, interval.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);
                _Intervals.Add(old.Start, old);
                _Intervals.Add(merged.Start, merged);
            }

            // If we can't check between this and the next one, continue.
            if (i >= overlapping.Count - 1)
                continue;

            // we know the next one is overlapping too,
            // but there's a hole that won't be plugged unless we do it now
            // [oi, oi, oi], i, i, i, [oi+1, oi+1, o1+1]
            var gap = overlapping[i + 1].Start - oi.End;
            if (gap > 1)
                _Intervals.Add(
                    oi.End + 1, new OrderedInterval<T>(oi.End + 1, overlapping[i + 1].Start - 1, interval.Data));
        }

        UpdateStartEndPositions();
        return modified;
    }

    /// <summary>
    /// Returns all overlapping intervals between the start and end position.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<OrderedInterval<T>> GetIntervals(int start, int end)
    {
        return GetIntervals(new OrderedInterval(start, end));
    }

    public List<OrderedInterval<T>> GetIntervals(OrderedInterval interval)
    {
        var overlapping = new List<OrderedInterval<T>>();

        if (!_Intervals.Any())
            return overlapping;

        var i0 = _Intervals.Keys.BinarySearchClosest(interval.Start);
        if (i0 >= 1 && _Intervals[_Intervals.Keys[i0 - 1]].Overlaps(interval))
            i0--;

        for (int i = i0; i < _Intervals.Count; i++)
        {
            if (_Intervals[_Intervals.Keys[i]].Overlaps(interval))
                overlapping.Add(_Intervals[_Intervals.Keys[i]]);
            else
                break;
        }

        return overlapping;
    }

    /// <summary>
    /// Returns the next interval after the given position
    /// </summary>
    /// <param name="position"></param>
    /// <param name="direction">Positive to the right, negative to the left</param>
    /// <returns></returns>
    public OrderedInterval<T>? GetNext(int position, int direction = 1)
    {
        if (!_Intervals.Any())
            return null;

        if (position > End && direction > 0 ||
            position < Start && direction < 0)
            return null;

        var i0 = _Intervals.Keys.BinarySearchIndexOf(position);
        if (i0 < 0)
        {
            i0 = ~i0;
            // move to either containing interval index or the one before
            if (direction < 0)
                i0--;
        }

        if (direction < 0 && _Intervals.Values[i0].Contains(position))
            i0--;

        if (i0 >= _Intervals.Count || i0 < 0)
            return null;

        return _Intervals[_Intervals.Keys[i0]];
    }

    public IList<OrderedInterval<T>> GetAllIntervals() => _Intervals.Values.ToList();

    /// <summary>
    /// Remove the interval from storage
    /// </summary>
    /// <param name="interval"></param>
    /// <returns>The ordered intervals that were removed during the process.</returns>
    public List<OrderedInterval<T>> Clear(int start, int end)
    {
        return Clear(new OrderedInterval(start, end));
    }

    /// <summary>
    /// Remove the interval from storage
    /// </summary>
    /// <param name="interval"></param>
    /// <returns>The ordered intervals that were removed during the process.</returns>
    public List<OrderedInterval<T>> Clear(OrderedInterval interval)
    {
        if (!_Intervals.Any())
            return new List<OrderedInterval<T>>();

        if (interval.End < Start || interval.Start > End)
            return new List<OrderedInterval<T>>();

        var i0 = _Intervals.Keys.BinarySearchIndexOf(interval.Start);
        if (i0 < 0)
            i0 = ~i0;

        // We now have either the interval or the one to the right of it

        OrderedInterval<T> currentInterval;
        // Start with a good guess of where the interval starts which is to the left
        // the one we have found (or the one that is greater than interval.start)
        if (i0 >= 1 && _Intervals[_Intervals.Keys[i0 - 1]].Overlaps(interval))
        {
            i0--;
        }

        // three situations could occur
        // 1. interval partially overlaps and interval.start is to the right of the other interval's start
        // 2. interval partially overlaps and interval.end is to the left of the other interval's end
        // 3. interval contains the other interval entirely.
        // In case 3 we remove the other interval from the list
        // In case 1 & 2 we shorten the other intervals by the overlapping amount. 1 = splitRight, 2 = splitLeft.

        // intervals to remove
        List<OrderedInterval<T>> removed = new();
        // The interval to split left (if any)
        OrderedInterval<T>? splitLeft = null;
        // The interval to split right (if any). Note splitLeft may be equal to split right.
        OrderedInterval<T>? splitRight = null;

        for (int i = i0; i < _Intervals.Count; i++)
        {
            var existingInterval = _Intervals[_Intervals.Keys[i]];
            if (!existingInterval.Overlaps(interval))
                break;

            if (interval.Contains(existingInterval))
            {
                removed.Add(existingInterval);
                continue;
            }

            if (interval.Start > existingInterval.Start)
                splitRight = existingInterval;

            if (interval.End < existingInterval.End)
                splitLeft = existingInterval;
        }

        foreach (var intervalToRemove in removed)
            _Intervals.Remove(intervalToRemove.Start);

        // we need to work with split right first because split left may depend on it
        if (splitRight != null)
        {
            removed.Add(new OrderedInterval<T>(interval.Start, Math.Min(interval.End, splitRight.End),
                splitRight.Data.Clone()));
            _Intervals.Remove(splitRight.Start);
            _Intervals.Add(splitRight.Start,
                new OrderedInterval<T>(splitRight.Start, interval.Start - 1, splitRight.Data));
        }

        if (splitLeft != null)
        {
            if (splitLeft != splitRight) // we may have already removed split Right, so don't remove it twice
            {
                _Intervals.Remove(splitLeft.Start);
                removed.Add(new OrderedInterval<T>(Math.Max(splitLeft.Start, interval.Start), interval.End,
                    splitLeft.Data.Clone()));
            }

            _Intervals.Add(interval.End + 1, new OrderedInterval<T>(interval.End + 1, splitLeft.End, splitLeft.Data));
        }

        UpdateStartEndPositions();
        return removed;
    }

    /// <summary>
    /// Shifts all intervals to the right of <paramref name="from"/>, to the right by <paramref name="n"/>
    /// If from is inside an overlapping interval, the end gets extended
    /// If from is at the start of an overlapping interval, the interval is shifted right
    /// </summary>
    /// <param name="from">The position where everything to the right gets shifted right.</param>
    /// <param name="n"></param>
    public void ShiftRight(int from, int n)
    {
        var overlapping = this.GetIntervals(from, Math.Max(this.End, from));
        // need to work backwards so we don't end up with adding keys 
        // that already exist
        for (int i = overlapping.Count - 1; i >= 0; i--)
        {
            var oi = overlapping[i];
            if (oi.Start < from)
                oi.End += n;
            else
            {
                _Intervals.Remove(oi.Start);
                _Intervals.Add(oi.Start + n, new OrderedInterval<T>(oi.Start + n, oi.End + n, oi.Data));
            }
        }

        UpdateStartEndPositions();
    }

    /// <summary>
    /// Shifts all intervals to the right of <paramref name="from"/>, to the left by <paramref name="n"/>
    /// If from is inside an overlapping interval, the end gets contracted
    /// </summary>
    /// <param name="from">The position where everything to the right gets shifted left.</param>
    /// <param name="n"></param>
    public void ShiftLeft(int from, int n)
    {
        var removed = new List<OrderedInterval<T>>();

        var overlapping = this.GetIntervals(from, Math.Max(from, this.End));

        for (int i = 0; i < overlapping.Count; i++)
        {
            var oi = overlapping[i];
            if (oi.Start <= from)
            {
                if (oi.End - n >= oi.Start) // we end up with an interval of length 1 or greater
                    oi.End -= n;
                // Anything that is shifted to the left of from gets removed. This shouldn't
                // really happen in reality because when we are using it, it will be with a cut also
                else
                    _Intervals.Remove(oi.Start);
            }
            else
            {
                _Intervals.Remove(oi.Start);
                // if it doesn't move partially past from, we can just shift the whole thing.
                if (oi.Start - n >= from)
                    _Intervals.Add(oi.Start - n, new OrderedInterval<T>(oi.Start - n, oi.End - n, oi.Data));
                else
                {
                    _Intervals.Add(from, new OrderedInterval<T>(from, oi.End - n, oi.Data));
                }
            }
        }

        UpdateStartEndPositions();
    }

    private void UpdateStartEndPositions()
    {
        if (_Intervals.Any())
        {
            Start = _Intervals.First().Value.Start;
            End = _Intervals.Last().Value.End;
        }
    }

    public void AddRange(IEnumerable<OrderedInterval<T>> intervals)
    {
        foreach (var interval in intervals)
            Add(interval);
    }

    /// <summary>
    /// Clears all intervals
    /// </summary>
    public void Clear()
    {
        _Intervals.Clear();
    }
}