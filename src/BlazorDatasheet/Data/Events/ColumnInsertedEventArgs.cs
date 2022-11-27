namespace BlazorDatasheet.Data.Events;

public class ColumnInsertedEventArgs
{
    public int ColAfter { get; }

    public ColumnInsertedEventArgs(int colAfter)
    {
        ColAfter = colAfter;
    }
}