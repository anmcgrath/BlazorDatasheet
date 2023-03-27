namespace BlazorDatasheet.Events;

public class ColumnInsertedEventArgs
{
    public int ColAfter { get; }

    public ColumnInsertedEventArgs(int colAfter)
    {
        ColAfter = colAfter;
    }
}