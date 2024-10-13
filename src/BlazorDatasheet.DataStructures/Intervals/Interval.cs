namespace BlazorDatasheet.DataStructures.Intervals;

public readonly struct Interval
{
    public int Start { get; }
    public int End { get; }
    public int Size { get; }

    public Interval(int start, int end)
    {
        Start = start;
        End = end;
        Size = end - start + 1;
    }
}