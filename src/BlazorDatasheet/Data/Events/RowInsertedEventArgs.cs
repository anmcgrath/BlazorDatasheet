namespace BlazorDatasheet.Data.Events;

public class RowInsertedEventArgs
{
    public int Index { get; }

    public RowInsertedEventArgs(int index)
    {
        Index = index;
    }
}