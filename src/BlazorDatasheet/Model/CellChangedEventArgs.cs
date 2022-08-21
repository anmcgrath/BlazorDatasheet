namespace BlazorDatasheet.Model;

public class CellChangedEventArgs
{
    public Cell Cell { get; }
    public int Row { get; }
    public int Col { get; }

    public CellChangedEventArgs(Cell cell, int row, int col)
    {
        Cell = cell;
        Row = row;
        Col = col;
    }
}