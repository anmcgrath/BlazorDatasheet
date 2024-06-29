namespace BlazorDatasheet.DataStructures.Intervals;

/// <summary>
/// An ordered interval between its start and end position (inclusive)
/// Because the interval is ordered, the start position will always be less than the end position
/// </summary>
public class OrderedInterval
{
    private int _start;

    public int Start
    {
        get => _start;
        private set
        {
            _start = Math.Min(value, _end);
            _end = Math.Max(value, _end);
        }
    }

    private int _end;

    public int End
    {
        get => _end;
        private set
        {
            _start = Math.Min(value, _start);
            _end = Math.Max(value, _start);
        }
    }

    public int Length => End - Start + 1;

    public OrderedInterval(int start, int end)
    {
        _start = Math.Min(start, end);
        _end = Math.Max(start, end);
    }

    public bool Contains(int value)
    {
        return value >= _start && value <= _end;
    }

    /// <summary>
    /// Whether this interval fully contains the other interval
    /// </summary>
    /// <param name="interval"></param>
    /// <returns></returns>
    public bool Contains(OrderedInterval interval)
    {
        return Start <= interval.Start && End >= interval.End;
    }

    public bool Overlaps(OrderedInterval interval)
    {
        return this.Contains(interval.Start)
               || this.Contains(interval.End)
               || interval.Contains(this.End)
               || interval.Contains(this.Start);
    }

    public bool NextTo(OrderedInterval interval)
    {
        return (interval.Start - this.End) == 1 || (this.Start - interval.End) == 1;
    }

    /// <summary>
    /// Finds the smallest number of intervals that covers all the intervals.
    /// Essentially it removes and combines any overlapping intervals
    /// </summary>
    /// <param name="intervals"></param>
    /// <returns></returns>
    public static List<OrderedInterval> Merge(IEnumerable<OrderedInterval> intervals)
    {
        var sortedIntervals =
            intervals.Select(x => x.Copy())
                .OrderBy(x => x.Start)
                .ToList();
        var mergedIntervals = new List<OrderedInterval>();

        if (!sortedIntervals.Any())
            return mergedIntervals;

        if (sortedIntervals.Count == 1)
        {
            mergedIntervals.Add(sortedIntervals.First());
            return mergedIntervals;
        }

        var stack = new Stack<OrderedInterval>();
        stack.Push(sortedIntervals.First());

        for (int i = 1; i < sortedIntervals.Count; i++)
        {
            if (stack.Peek().Overlaps(sortedIntervals[i]) || stack.Peek().NextTo(sortedIntervals[i]))
                stack.Peek().End = Math.Max(stack.Peek().End, sortedIntervals[i].End);
            else
                stack.Push(sortedIntervals[i]);
        }

        return stack.ToList();
    }

    public OrderedInterval Copy()
    {
        return new OrderedInterval(Start, End);
    }

    public static List<OrderedInterval> Merge(params OrderedInterval[] intervals)
    {
        return Merge(intervals.AsEnumerable());
    }

    public void Shift(int n)
    {
        _start += n;
        _end += n;
        _start = Math.Min(_start, _end);
        _end = Math.Max(_start, _end);
    }

    public override string ToString()
    {
        return $"[{Start},{End}]";
    }
}

public class OrderedInterval<T> : OrderedInterval
{
    public T Data { get; set; }

    public OrderedInterval(int start, int end, T data) : base(start, end)
    {
        Data = data;
    }

    public new OrderedInterval<T> Copy()
    {
        return new OrderedInterval<T>(Start, End, Data);
    }

    public override string ToString()
    {
        return $"[{Start},{End}]:{Data.ToString()}";
    }
}