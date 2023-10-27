using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// Contains non-overlapping data in ranges
/// </summary>
public class Range1DStore<T>
{
    protected readonly NonOverlappingIntervals<OverwritingValue<T>> _intervals;
    private readonly T? _defaultIfNotFound;

    public Range1DStore(T? defaultIfNotFound)
    {
        _defaultIfNotFound = defaultIfNotFound;
        _intervals = new NonOverlappingIntervals<OverwritingValue<T>>(new OverwritingValue<T>(defaultIfNotFound));
    }

    /// <summary>
    /// Assigns the range to the value given. Returns any ranges that were modified when it was set.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual List<(int stat, int end, T value)> Set(int start, int end, T value)
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
    /// Cuts the range from the store and returns any ranges that were removed to so so.
    /// Shifts the remaining values to the left.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public virtual List<(int start, int end, T value)> Cut(int start, int end)
    {
        var removed = _intervals.Remove(start, end).Select(x => (x.Start, x.End, x.Data.Value)).ToList();
        _intervals.ShiftLeft(start, (start - end) + 1);
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