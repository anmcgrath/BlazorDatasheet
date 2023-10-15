namespace BlazorDatasheet.Events.Layout;

public class RowHeightChangedEventArgs
{
    public int RowIndex { get; }
    public double NewWidth { get; }
    public double OldWidth { get; }

    public RowHeightChangedEventArgs(int rowIndex, double newWidth, double oldWidth)
    {
        RowIndex = rowIndex;
        NewWidth = newWidth;
        OldWidth = oldWidth;
    }
}