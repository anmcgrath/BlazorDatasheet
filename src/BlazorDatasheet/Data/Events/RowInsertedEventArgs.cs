namespace BlazorDatasheet.Data.Events;

public class RowInsertedEventArgs
{
    /// <summary>
    /// The index that the row was inserted after
    /// </summary>
    public int IndexAfter { get; }

    public RowInsertedEventArgs(int indexAfter)
    {
        IndexAfter = indexAfter;
    }
}