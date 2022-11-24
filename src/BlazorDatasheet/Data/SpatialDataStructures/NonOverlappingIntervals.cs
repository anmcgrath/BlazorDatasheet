namespace BlazorDatasheet.Data.SpatialDataStructures;

using BlazorDatasheet.Util;

/// <summary>
/// A set of non-overlapping intervals containing some mergeable data.
/// When a new interval is added, the data from that interval is merged into any existing.
/// </summary>
/// <typeparam name="T"></typeparam>
public class NonOverlappingIntervals<T> where T : IMergeable<T>
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
    /// Returns the data (if any) associated with the interval containing the position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public T? Get(int position)
    {
        if (!_Intervals.Any())
            return default(T);

        if (position < Start || position > End)
            return default(T);

        var i0 = _Intervals.Keys.BinarySearchIndexOf(position);
        if (i0 < 0)
            i0 = ~i0; // closest value to it (with start index greater than it)
        else
            return _Intervals[_Intervals.Keys[i0]].Data;

        // Now we have the next closest with a start index greater position
        // but it can't be that interval because position < start.
        // so we check the one before i0 to see if position is contained
        if (i0 - 1 < 0)
            return default(T);

        if (_Intervals[_Intervals.Keys[i0 - 1]].Contains(position))
            return _Intervals[_Intervals.Keys[i0 - 1]].Data;

        return default(T);
    }

    /// <summary>
    /// Adds the interval and merges any overlapping data into it.
    /// </summary>
    /// <param name="interval"></param>
    public void Add(OrderedInterval<T> interval)
    {
        Start = Math.Min(interval.Start, Start);
        End = Math.Max(interval.End, End);

        var overlapping = GetOverlappingIntervals(interval);
        if (!overlapping.Any())
        {
            _Intervals.Add(interval.Start, interval);
            return;
        }

        // Handle when interval extends before the first overlapping interval
        if (interval.Start < overlapping.First().Start)
            _Intervals.Add(interval.Start,
                           new OrderedInterval<T>(interval.Start, overlapping.First().Start - 1, interval.Data));
        // Handle when interval extends after the last overlapping interval
        if (interval.End > overlapping.Last().End)
            _Intervals.Add(overlapping.Last().End + 1,
                           new OrderedInterval<T>(overlapping.Last().End + 1, interval.End, interval.Data));

        for (int i = 0; i < overlapping.Count; i++)
        {
            var oi = overlapping[i];
            if (interval.Contains(oi))
            {
                oi.Data = oi.Data.Clone();
                oi.Data.Merge(interval.Data);
            }

            else if (oi.Contains(interval))
            {
                // We have [o, o, i, i, o, o] where o = overlapping interval
                // and i = interval we are adding
                // we remove o and add o0, and o1 so that we now have
                // [o0, o0, i, i, o1, o1]
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
                _Intervals.Remove(oi.Start);
                var old = new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data);
                var merged = new OrderedInterval<T>(interval.Start, oi.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);
                _Intervals.Add(old.Start, old);
                _Intervals.Add(merged.Start, merged);
            }
            else if (interval.End < oi.End)
            {
                _Intervals.Remove(oi.Start);
                var old = new OrderedInterval<T>(interval.End + 1, oi.End, oi.Data);
                var merged = new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data.Clone());
                old.Data.Merge(interval.Data);
                _Intervals.Add(old.Start, old);
                _Intervals.Add(merged.Start, merged);
            }

            // If we can't check between this and the next one, continue.
            if (i >= overlapping.Count - 1)
                continue;

            var gap = overlapping[i + 1].Start - oi.End;
            if (gap > 1)
                _Intervals.Add(
                    oi.End + 1, new OrderedInterval<T>(oi.End + 1, overlapping[i + 1].Start - 1, interval.Data));
        }
    }

    public List<OrderedInterval<T>> GetOverlappingIntervals(OrderedInterval interval)
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

    public IList<OrderedInterval<T>> GetAllIntervals() => _Intervals.Values.ToList();

    /// <summary>
    /// Deletes the interval from storage
    /// </summary>
    /// <param name="interval"></param>
    public void Delete(OrderedInterval interval)
    {
        if (!_Intervals.Any())
            return;

        if (interval.End < Start || interval.Start > End)
            return;

        var i0 = _Intervals.Keys.BinarySearchIndexOf(interval.Start);
        if (i0 < 0)
            i0 = ~i0;

        // We now have either the 

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
        List<OrderedInterval<T>> toRemove = new();
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
                toRemove.Add(existingInterval);
                continue;
            }

            if (interval.Start > existingInterval.Start)
                splitRight = existingInterval;

            if (interval.End < existingInterval.End)
                splitLeft = existingInterval;
        }

        foreach (var intervalToRemove in toRemove)
            _Intervals.Remove(intervalToRemove.Start);

        // we need to work with split right first because split left may depend on it
        if (splitRight != null)
        {
            _Intervals.Remove(splitRight.Start);
            _Intervals.Add(splitRight.Start,
                           new OrderedInterval<T>(splitRight.Start, interval.Start - 1, splitRight.Data));
        }

        if (splitLeft != null)
        {
            if (splitLeft != splitRight)
                _Intervals.Remove(splitLeft.Start);
            _Intervals.Add(interval.End + 1, new OrderedInterval<T>(interval.End + 1, splitLeft.End, splitLeft.Data));
        }

        // Update start and end positions of store
        if (_Intervals.Any())
        {
            Start = _Intervals.First().Value.Start;
            End = _Intervals.Last().Value.End;
        }
    }

    /// <summary>
    /// Clones all intervals (including data) and returns the list of cloned intervals
    /// </summary>
    /// <returns></returns>
    public IEnumerable<OrderedInterval<T>> CloneAllIntervals()
    {
        var clones =
            _Intervals
                .Values
                .Select(x => new OrderedInterval<T>(x.Start, x.End, x.Data.Clone()));
        return clones.ToList();
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