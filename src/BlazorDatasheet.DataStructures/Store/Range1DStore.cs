using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// Contains non-overlapping data in ranges
/// </summary>
public class Range1DStore<T>
{
    protected readonly MergeableIntervalStore<OverwritingValue<T>> Intervals;
    private readonly T? _defaultIfNotFound;

    public Range1DStore(T? defaultIfNotFound)
    {
        _defaultIfNotFound = defaultIfNotFound;
        Intervals = new MergeableIntervalStore<OverwritingValue<T>>(new OverwritingValue<T>(defaultIfNotFound));
    }

    /// <summary>
    /// Assigns the range to the value given. Returns any ranges that were modified when it was set.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual List<(int start, int end, T value)> Set(int start, int end, T value)
    {
        var modified = Intervals.Add(start, end, new OverwritingValue<T>(value));
        return modified.Select(x => (x.Start, x.End, x.Data.Value)).ToList();
    }

    /// <summary>
    /// Sets a range of length = 1 to the value given
    /// </summary>
    /// <param name="start"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public List<(int start, int end, T value)> Set(int start, T value)
    {
        return Set(start, start, value);
    }

    /// <summary>
    /// Removes the intervals between and including <paramref name="start"/> amd <paramref name="end"/>
    /// and shifts the remaining values to the left.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public virtual List<(int start, int end, T value)> Delete(int start, int end)
    {
        var removed = Intervals.Clear(start, end).Select(x => (x.Start, x.End, x.Data.Value)).ToList();
        Intervals.ShiftLeft(start, (end - start) + 1);
        return removed;
    }

    public List<(int start, int end, T? value)> GetOverlapping(int start, int end)
    {
        return Intervals.GetIntervals(start, end)
            .Select(x => (x.Start, x.End, x.Data.Value))
            .ToList();
    }

    /// <summary>
    /// Inserts empty values into the store and shifts to the right
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    public virtual void InsertAt(int start, int n)
    {
        Intervals.ShiftRight(start, n);
    }

    public T? Get(int position)
    {
        var val = Intervals.Get(position);
        if (val == null)
            return _defaultIfNotFound;
        return val.Value;
    }

    public virtual void BatchSet(List<(int start, int end, T data)> data)
    {
        foreach (var d in data)
            this.Set(d.start, d.end, d.data);
    }

    /// <summary>
    /// Returns the interval after the given position
    /// </summary>
    /// <param name="position"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public (int position, int end, T? data)? GetNext(int position, int direction = 1)
    {
        var interval = Intervals.GetNext(position, direction);
        if (interval == null)
            return null;

        return (interval.Start, interval.End, interval.Data.Value);
    }

    public List<(int start, int end, T? data)> GetAllIntervals()
    {
        return Intervals.GetAllIntervals().Select(x => (x.Start, x.End, x.Data.Value)).ToList();
    }

    public (int start, int end, T? data) GetInterval(int position)
    {
        var interval = Intervals.GetIntervals(new OrderedInterval(position, position)).FirstOrDefault();
        if (interval == null)
            return (-1, -1, _defaultIfNotFound);

        return (interval.Start, interval.End, interval.Data.Value);
    }

    /// <summary>
    /// Removes the data between the given positions but does not shift the remaining data
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public List<(int start, int end, T? value)> Clear(int start, int end)
    {
        return Intervals.Clear(start, end).Select(x => (x.Start, x.End, x.Data.Value)).ToList();
    }

    public void Clear()
    {
        this.Intervals.Clear();
    }
}

public class OverwritingValue<R> : IMergeable<OverwritingValue<R>>
{
    public R? Value { get; private set; }

    public OverwritingValue(R? value)
    {
        Value = value;
    }

    public void Merge(OverwritingValue<R> item)
    {
        Value = item.Value;
    }

    public OverwritingValue<R> Clone()
    {
        return new OverwritingValue<R>(Value);
    }
}