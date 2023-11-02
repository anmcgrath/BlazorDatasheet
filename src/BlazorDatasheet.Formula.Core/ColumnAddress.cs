namespace BlazorDatasheet.Formula.Core;

public class ColumnAddress
{
    public ColumnAddress(int start, int end)
    {
        Start = start;
        End = end;
    }

    public int Start { get; }
    public int End { get; }
}