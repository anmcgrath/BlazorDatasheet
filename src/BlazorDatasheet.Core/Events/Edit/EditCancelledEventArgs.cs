namespace BlazorDatasheet.Core.Events.Edit;

public class EditCancelledEventArgs
{
    public EditCancelledEventArgs(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public int Row { get; }
    public int Col { get; }
}