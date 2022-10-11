namespace BlazorDatasheet.Edit.Events;

public class RejectEditEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public object InitialValue { get; }
    public object EditedValue { get; }

    public RejectEditEventArgs(int row, int col, object initialValue, object editedValue)
    {
        Row = row;
        Col = col;
        InitialValue = initialValue;
        EditedValue = editedValue;
    }
}