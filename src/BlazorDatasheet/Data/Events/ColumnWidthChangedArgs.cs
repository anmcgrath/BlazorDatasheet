namespace BlazorDatasheet.Data.Events;

public class ColumnWidthChangedArgs
{
    public ColumnWidthChangedArgs(int column, double newWidth, double oldWidth)
    {
        Column = column;
        NewWidth = newWidth;
        OldWidth = oldWidth;
    }

    public int Column { get; }
    public double NewWidth { get; }
    public double OldWidth { get; }
}