namespace BlazorDatasheet.Events.Layout;

public class RowInsertedEventArgs
{
    /// <summary>
    /// The index of the new row
    public int Index { get; }

    public int NRows { get; }

    public RowInsertedEventArgs(int index, int nRows)
    {
        Index = index;
        NRows = nRows;
    }
}