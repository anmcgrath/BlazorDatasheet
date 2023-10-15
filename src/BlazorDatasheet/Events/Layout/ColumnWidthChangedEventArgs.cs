namespace BlazorDatasheet.Events.Layout;

public class ColumnWidthChangedEventArgs
{
    public ColumnWidthChangedEventArgs(int columnIndex, double newWidth, double oldWidth)
    {
        ColumnIndex = columnIndex;
        NewWidth = newWidth;
        OldWidth = oldWidth;
    }

    public int ColumnIndex { get; }
    public double NewWidth { get; }
    public double OldWidth { get; }
}