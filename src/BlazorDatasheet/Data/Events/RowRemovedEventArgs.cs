namespace BlazorDatasheet.Data.Events;

public class RowRemovedEventArgs
{
    public int Index { get; }
    public Row Row { get; }

    public RowRemovedEventArgs(int index, Row row)
    {
        Index = index;
        Row = row;
    }
}