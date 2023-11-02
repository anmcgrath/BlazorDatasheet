namespace BlazorDatasheet.Core.Events.Layout;

public class RowHeightChangedEventArgs
{
    public int RowIndexStart { get; }
    public double NewHeight { get; }
    public double RowIndexEnd { get; }

    public RowHeightChangedEventArgs(int rowIndexStart, double rowIndexEnd, double newHeight)
    {
        RowIndexStart = rowIndexStart;
        NewHeight = newHeight;
        RowIndexEnd = rowIndexEnd;
    }
}