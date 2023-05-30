namespace BlazorDatasheet.Events;

public class ColumnInsertedEventArgs
{
    public int ColAfter { get; }
    public double? Width { get; }

    public ColumnInsertedEventArgs(int colAfter, double? width)
    {
        ColAfter = colAfter;
        Width = width;
    }
}