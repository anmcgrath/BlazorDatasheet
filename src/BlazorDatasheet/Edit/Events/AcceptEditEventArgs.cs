namespace BlazorDatasheet.Edit.Events;

public class AcceptEditEventArgs
{
    public readonly int Row;
    public readonly int Col;
    public readonly object OldValue;
    public readonly object NewValue;
    public readonly bool Accepted;

    public AcceptEditEventArgs(int row, int col, object oldValue, object newValue)
    {
        Row = row;
        Col = col;
        OldValue = oldValue;
        NewValue = newValue;
    }
}