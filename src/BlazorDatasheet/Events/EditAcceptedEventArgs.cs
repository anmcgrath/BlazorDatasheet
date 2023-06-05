namespace BlazorDatasheet.Events;

public class EditAcceptedEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public object? Value { get; }

    public EditAcceptedEventArgs(int row, int col, object? value)
    {
        Row = row;
        Col = col;
        Value = value;
    }
}