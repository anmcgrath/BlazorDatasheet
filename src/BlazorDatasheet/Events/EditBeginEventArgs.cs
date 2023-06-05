namespace BlazorDatasheet.Events;

public class EditBeginEventArgs
{
    public EditBeginEventArgs(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public int Row { get; }
    public int Col { get; }
}