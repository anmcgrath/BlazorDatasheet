namespace BlazorDatasheet.Events;

public class ColumnWidthChangedEventArgs
{
    public ColumnWidthChangedEventArgs(int column, double newWidth, double oldWidth)
    {
        Column = column;
        NewWidth = newWidth;
        OldWidth = oldWidth;
    }

    public int Column { get; }
    public double NewWidth { get; }
    public double OldWidth { get; }
}