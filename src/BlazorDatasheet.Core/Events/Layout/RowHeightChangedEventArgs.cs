namespace BlazorDatasheet.Core.Events.Layout;

public class RowHeightChangedEventArgs
{
    public int RowIndexStart { get; }
    public double RowIndexEnd { get; }

    public RowHeightChangedEventArgs(int rowIndexStart, double rowIndexEnd)
    {
        RowIndexStart = rowIndexStart;
        RowIndexEnd = rowIndexEnd;
    }
}