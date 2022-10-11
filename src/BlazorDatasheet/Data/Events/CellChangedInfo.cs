namespace BlazorDatasheet.Data.Events;

public class CellChangedInfo
{
    public Cell Cell { get; }
    public int Row { get; }
    public int Column { get; }

    public CellChangedInfo(Cell cell, int row, int column)
    {
        Cell = cell;
        Row = row;
        Column = column;
    }
}