using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// Contains non-overlapping data in ranges
/// </summary>
public class Range1DStore<T>
{
    protected readonly MergeableIntervalStore<OverwritingValue<T>> _intervals;
    private readonly T? _defaultIfNotFound;

    public Range1DStore(T? defaultIfNotFound)
    {
        _defaultIfNotFound = defaultIfNotFound;
        _intervals = new MergeableIntervalStore<OverwritingValue<T>>(new OverwritingValue<T>(defaultIfNotFound));
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
        var modified = _intervals.Add(start, end, new OverwritingValue<T>(value));
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
        var removed = _intervals.Clear(start, end).Select(x => (x.Start, x.End, x.Data.Value)).ToList();
        _intervals.ShiftLeft(start, (end - start) + 1);
        return removed;
    }

    /// <summary>
    /// Inserts empty values into the store and shifts to the right
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    public virtual void InsertAt(int start, int n)
    {
        _intervals.ShiftRight(start, n);
    }

    public T? Get(int position)
    {
        var val = _intervals.Get(position);
        if (val == null)
            return _defaultIfNotFound;
        return val.Value;
    }

    public virtual void BatchSet(List<(int start, int end, T data)> data)
    {
        foreach (var d in data)
            this.Set(d.start, d.end, d.data);
    }

    public List<(int start, int end, T? data)> GetAllIntervals()
    {
        return _intervals.GetAllIntervals().Select(x => (x.Start, x.End, x.Data.Value)).ToList();
    }

    public (int start, int end, T? data) GetInterval(int position)
    {
        var interval = _intervals.GetIntervals(new OrderedInterval(position, position)).FirstOrDefault();
        if (interval == null)
            return (-1, -1, _defaultIfNotFound);

        return (interval.Start, interval.End, interval.Data.Value);
    }

    public void Clear()
    {
        this._intervals.Clear();
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