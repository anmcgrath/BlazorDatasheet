namespace BlazorDatasheet.Core.Events.Layout;

public class ColumnWidthChangedEventArgs
{
    public ColumnWidthChangedEventArgs(int colStart, int colEnd, double newWidth)
    {
        ColStart = colStart;
        NewWidth = newWidth;
        ColEnd = colEnd;
    }

    public int ColStart { get; }
    public double NewWidth { get; }
    public int ColEnd { get; }
}