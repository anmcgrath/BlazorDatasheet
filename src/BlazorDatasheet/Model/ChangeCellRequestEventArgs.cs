namespace BlazorDatasheet.Model;

public class ChangeCellRequestEventArgs
{
    public ChangeCellRequestEventArgs(int row, int col, string newValue)
    {
        Row = row;
        Col = col;
        NewValue = newValue;
    }

    public int Row { get; }
    public int Col { get; }
    public string NewValue { get; set; }
}