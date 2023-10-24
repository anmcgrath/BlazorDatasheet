namespace BlazorDatasheet.Core.Events.Edit;

public class EditFinishedEventArgs
{
    public EditFinishedEventArgs(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public int Row { get; }
    public int Col { get; }
}