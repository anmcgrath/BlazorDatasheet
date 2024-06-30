using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Search;
using BlazorDatasheet.DataStructures.Store;

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
    private readonly SortedList<int, OrderedInterval<T>> _intervals = new();

    /// <summary>
    /// The default value returned if there is no value found.
    /// </summary>
    private readonly T? _defaultValue;

    public MergeableIntervalStore(T? defaultValue = default(T))
    {
        _defaultValue = defaultValue;
    }

    /// <summary>
    /// Whether we have any intervals stored.
    /// </summary>
    /// <returns></returns>
    public bool Any() => _intervals.Count != 0;

    /// <summary>
    /// Returns the data (if any) associated with the interval containing the position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public T? Get(int position)
    {
        if (_intervals.Count == 0)
            return _defaultValue;

        if (position < Start || position > End)
            return _defaultValue;

        var i0 = _intervals.Keys.BinarySearchIndexOf(position);
        if (i0 < 0)
            i0 = ~i0; // closest value to it (with start index greater than it)
        else
            return _intervals[_intervals.Keys[i0]].Data;

        // Now we have the next closest with a start index greater position
        // but it can't be that interval because position < start.
        // so we check the one before i0 to see if position is contained
        if (i0 - 1 < 0)
            return _defaultValue;

        if (_intervals[_intervals.Keys[i0 - 1]].Contains(position))
            return _intervals[_intervals.Keys[i0 - 1]].Data;

        return _defaultValue;
    }

    public MergeableIntervalStoreRestoreData<T> Add(int start, int end, T value)
    {
        return Add(new OrderedInterval<T>(start, end, value));
    }

    /// <summary>
    /// Adds the interval and merges any overlapping data into it.
    /// </summary>
    /// <param name="interval"></param>
    /// <returns>Intervals that were either modified or removed while adding</returns>
    public MergeableIntervalStoreRestoreData<T> Add(OrderedInterval<T> interval)
    {
        Start = Math.Min(interval.Start, Start);
        End = Math.Max(interval.End, End);

        var overlapping = GetIntervals(interval);
        if (overlapping.Count == 0)
        {
            _intervals.Add(interval.Start, interval);
            UpdateStartEndPositions();
            return new MergeableIntervalStoreRestoreData<T>()
            {
                AddedIntervals = { interval }
            };
        }

        var restoreData = new MergeableIntervalStoreRestoreData<T>();
        var intervalsToAdd = new List<OrderedInterval<T>>();

        // Handle when interval extends before the first overlapping interval
        if (interval.Start < overlapping.First().Start)
            intervalsToAdd.Add(new OrderedInterval<T>(interval.Start, overlapping.First().Start - 1, interval.Data));

        // Handle when interval extends after the last overlapping interval
        if (interval.End > overlapping.Last().End)
            intervalsToAdd.Add(new OrderedInterval<T>(overlapping.Last().End + 1, interval.End, interval.Data));

        for (int i = 0; i < overlapping.Count; i++)
        {
            var oi = overlapping[i];
            if (interval.Contains(oi))
            {
                // remove the existing, add a new interval with the merged data
                var clone = new OrderedInterval<T>(oi.Start, oi.End, oi.Data.Clone());
                clone.Data.Merge(interval.Data);
                intervalsToAdd.Add(clone);

                _intervals.Remove(oi.Start);
                restoreData.RemovedIntervals.Add(oi);
            }

            else if (oi.Contains(interval))
            {
                // We have [o, o, i, i, o, o] where o = overlapping interval
                // and i = interval we are adding
                // we remove o and add o0, and o1 so that we now have
                // [o0, o0, i, i, o1, o1]

                // first store the (removed) original data that was in o.
                restoreData.RemovedIntervals.Add(oi);
                _intervals.Remove(oi.Start);

                if (oi.Start != interval.Start) // add a interval before the merged interval
                    intervalsToAdd.Add(new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data.Clone()));

                var merged = new OrderedInterval<T>(interval.Start, interval.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);
                intervalsToAdd.Add(merged);

                if (oi.End != interval.End) // add an interval after the merged interval
                    intervalsToAdd.Add(new OrderedInterval<T>(interval.End + 1, oi.End, oi.Data.Clone()));
            }

            else if (interval.Start > oi.Start)
            {
                // [o, o, i, i] i, i
                // first store the (removed) original data from o
                restoreData.RemovedIntervals.Add(oi);
                _intervals.Remove(oi.Start);

                var old = new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data.Clone());
                var merged = new OrderedInterval<T>(interval.Start, oi.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);

                intervalsToAdd.Add(old);
                intervalsToAdd.Add(merged);
            }
            else if (interval.End < oi.End)
            {
                // i, i [i, i, o, o] 
                // first store the (removed) original data from o
                restoreData.RemovedIntervals.Add(oi);
                _intervals.Remove(oi.Start);

                var old = new OrderedInterval<T>(interval.End + 1, oi.End, oi.Data.Clone());
                var merged = new OrderedInterval<T>(oi.Start, interval.End, oi.Data.Clone());
                merged.Data.Merge(interval.Data);

                intervalsToAdd.Add(old);
                intervalsToAdd.Add(merged);
            }

            // If we can't check between this and the next one, continue.
            if (i >= overlapping.Count - 1)
                continue;

            // we know the next one is overlapping too,
            // but there's a hole that won't be plugged unless we do it now
            // [oi, oi, oi], i, i, i, [oi+1, oi+1, o1+1]
            var gap = overlapping[i + 1].Start - oi.End;
            if (gap > 1)
                intervalsToAdd.Add(new OrderedInterval<T>(oi.End + 1, overlapping[i + 1].Start - 1, interval.Data));
        }

        foreach (var newOi in intervalsToAdd)
            _intervals.Add(newOi.Start, newOi);

        restoreData.AddedIntervals.AddRange(intervalsToAdd);

        UpdateStartEndPositions();
        return restoreData;
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

        if (!_intervals.Any())
            return overlapping;

        var i0 = _intervals.Keys.BinarySearchClosest(interval.Start);
        if (i0 >= 1 && _intervals[_intervals.Keys[i0 - 1]].Overlaps(interval))
            i0--;

        for (int i = i0; i < _intervals.Count; i++)
        {
            if (_intervals[_intervals.Keys[i]].Overlaps(interval))
                overlapping.Add(_intervals[_intervals.Keys[i]]);
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
        if (!_intervals.Any())
            return null;

        if (position > End && direction > 0 ||
            position < Start && direction < 0)
            return null;

        var i0 = _intervals.Keys.BinarySearchIndexOf(position);
        if (i0 < 0)
        {
            i0 = ~i0;
            // move to either containing interval index or the one before
            if (direction < 0)
                i0--;
        }

        if (direction < 0 && _intervals.Values[i0].Contains(position))
            i0--;

        if (i0 >= _intervals.Count || i0 < 0)
            return null;

        return _intervals[_intervals.Keys[i0]];
    }

    public IList<OrderedInterval<T>> GetAllIntervals() => _intervals.Values.ToList();

    /// <summary>
    /// Remove the interval from storage
    /// </summary>
    /// <param name="interval"></param>
    /// <returns>The ordered intervals that were removed during the process.</returns>
    public MergeableIntervalStoreRestoreData<T> Clear(int start, int end)
    {
        return Clear(new OrderedInterval(start, end));
    }

    /// <summary>
    /// Remove the interval from storage
    /// </summary>
    /// <param name="interval"></param>
    /// <returns>The ordered intervals that were removed during the process.</returns>
    public MergeableIntervalStoreRestoreData<T> Clear(OrderedInterval interval)
    {
        if (!_intervals.Any())
            return new MergeableIntervalStoreRestoreData<T>();

        if (interval.End < Start || interval.Start > End)
            return new MergeableIntervalStoreRestoreData<T>();

        var overlapping = GetIntervals(interval.Start, interval.End);
        if (overlapping.Count == 0)
            return new MergeableIntervalStoreRestoreData<T>();

        var restoreData = new MergeableIntervalStoreRestoreData<T>();
        var intervalsToAdd = new List<OrderedInterval<T>>();

        foreach (var oi in overlapping)
        {
            restoreData.RemovedIntervals.Add(oi);
            _intervals.Remove(oi.Start);

            if (interval.Contains(oi))
                continue;

            if (interval.Start > oi.Start)
                intervalsToAdd.Add(new OrderedInterval<T>(oi.Start, interval.Start - 1, oi.Data.Clone()));

            if (oi.End > interval.End)
                intervalsToAdd.Add(new OrderedInterval<T>(interval.End + 1, oi.End, oi.Data.Clone()));
        }

        foreach (var oi in intervalsToAdd)
            _intervals.Add(oi.Start, oi);

        restoreData.AddedIntervals.AddRange(intervalsToAdd);
        UpdateStartEndPositions();
        return restoreData;
    }

    /// <summary>
    /// Shifts all intervals to the right of <paramref name="from"/>, to the right by <paramref name="n"/>
    /// If from is inside an overlapping interval, the end gets extended
    /// If from is at the start of an overlapping interval, the interval is shifted right
    /// </summary>
    /// <param name="from">The position where everything to the right gets shifted right.</param>
    /// <param name="n"></param>
    public MergeableIntervalStoreRestoreData<T> ShiftRight(int from, int n)
    {
        var restoreData = new MergeableIntervalStoreRestoreData<T>()
        {
            Shifts = new List<AppliedShift>() { new AppliedShift(Axis.None, from, +n) }
        };

        var overlapping = this.GetIntervals(from, Math.Max(this.End, from));
        // need to work backwards so we don't end up with adding keys 
        // that already exist
        for (int i = overlapping.Count - 1; i >= 0; i--)
        {
            var oi = overlapping[i];
            if (oi.Start >= from)
            {
                _intervals.Remove(oi.Start);
                oi.Shift(n);
                _intervals.Add(oi.Start, oi);
            }
            else
            {
                restoreData.RemovedIntervals.Add(oi);
                _intervals.Remove(oi.Start);
                var newOi = new OrderedInterval<T>(oi.Start, oi.End + n, oi.Data.Clone());
                _intervals.Add(newOi.Start, newOi);
                restoreData.AddedIntervals.Add(newOi);
            }
        }

        UpdateStartEndPositions();
        return restoreData;
    }

    /// <summary>
    /// Shifts all intervals to the right of <paramref name="from"/>, to the left by <paramref name="n"/>
    /// If from is inside an overlapping interval, the end gets contracted
    /// </summary>
    /// <param name="from">The position where everything to the right gets shifted left.</param>
    /// <param name="n"></param>
    public MergeableIntervalStoreRestoreData<T> ShiftLeft(int from, int n)
    {
        var restoreData = new MergeableIntervalStoreRestoreData<T>()
        {
            Shifts = new List<AppliedShift>() { new AppliedShift(Axis.None, from, -n) }
        };

        var overlapping = this.GetIntervals(from, Math.Max(from, this.End));

        for (int i = 0; i < overlapping.Count; i++)
        {
            var oi = overlapping[i];
            if (oi.Start >= from)
            {
                _intervals.Remove(oi.Start);
                oi.Shift(-n);
                _intervals.Add(oi.Start, oi);
            }

            else
            {
                restoreData.RemovedIntervals.Add(oi);
                _intervals.Remove(oi.Start);
                var newOi = new OrderedInterval<T>(oi.Start, oi.End - n, oi.Data.Clone());
                _intervals.Add(newOi.Start, newOi);
                restoreData.AddedIntervals.Add(newOi);
            }
        }

        UpdateStartEndPositions();
        return restoreData;
    }

    private void UpdateStartEndPositions()
    {
        if (_intervals.Any())
        {
            Start = _intervals.First().Value.Start;
            End = _intervals.Last().Value.End;
        }
    }

    /// <summary>
    /// Clears all intervals
    /// </summary>
    public void Clear()
    {
        _intervals.Clear();
    }

    public virtual void Restore(MergeableIntervalStoreRestoreData<T> restoreData)
    {
        foreach (var shift in restoreData.Shifts)
        {
            foreach (var interval in GetIntervals(shift.Index, int.MaxValue))
            {
                _intervals.Remove(interval.Start);
                interval.Shift(-shift.Amount);
                _intervals.Add(interval.Start, interval);
            }
        }

        foreach (var added in restoreData.AddedIntervals)
        {
            _intervals.Remove(added.Start);
        }

        foreach (var removed in restoreData.RemovedIntervals)
        {
            _intervals.Add(removed.Start, removed);
        }

        this.UpdateStartEndPositions();
    }
}

public class MergeableIntervalStoreRestoreData<T>
{
    public List<OrderedInterval<T>> RemovedIntervals { get; internal set; } = new();
    public List<OrderedInterval<T>> AddedIntervals { get; internal set; } = new();

    public List<AppliedShift> Shifts { get; internal set; } = new();

    public void Merge(MergeableIntervalStoreRestoreData<T> other)
    {
        RemovedIntervals.AddRange(other.RemovedIntervals);
        AddedIntervals.AddRange(other.AddedIntervals);
        Shifts.AddRange(other.Shifts);
    }
}