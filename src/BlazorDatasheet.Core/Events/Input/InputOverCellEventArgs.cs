namespace BlazorDatasheet.Core.Events.Input;

public class InputOverCellEventArgs
{
    public int Row { get; }
    public int Col { get; }

    public bool PreventDefault { get; set; } = false;

    public InputOverCellEventArgs(int row, int col)
    {
        Row = row;
        Col = col;
    }
}