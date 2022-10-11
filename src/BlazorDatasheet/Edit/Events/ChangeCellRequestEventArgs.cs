namespace BlazorDatasheet.Edit.Events;

public class ChangeCellRequestEventArgs
{
    public ChangeCellRequestEventArgs(int row, int col, object newValue)
    {
        Row = row;
        Col = col;
        NewValue = newValue;
    }

    public int Row { get; }
    public int Col { get; }
    public object NewValue { get; set; }
}