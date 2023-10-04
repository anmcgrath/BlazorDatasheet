namespace BlazorDatasheet.Events;

public class RowRemovedEventArgs
{
    public int Index { get; }
    public int NRows { get; }

    public RowRemovedEventArgs(int index, int nRows)
    {
        Index = index;
        NRows = nRows;
    }
}