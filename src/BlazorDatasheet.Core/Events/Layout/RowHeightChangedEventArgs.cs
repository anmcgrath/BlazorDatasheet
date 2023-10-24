namespace BlazorDatasheet.Core.Events.Layout;

public class RowHeightChangedEventArgs
{
    public int RowIndexStart { get; }
    public double NewWidth { get; }
    public double RowIndexEnd { get; }

    public RowHeightChangedEventArgs(int rowIndexStart, double newWidth, double rowIndexEnd)
    {
        RowIndexStart = rowIndexStart;
        NewWidth = newWidth;
        RowIndexEnd = rowIndexEnd;
    }
}