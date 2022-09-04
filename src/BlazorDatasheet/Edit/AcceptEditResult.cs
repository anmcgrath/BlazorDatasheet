using BlazorDatasheet.Model;

namespace BlazorDatasheet.Edit;

public class AcceptEditResult
{
    public readonly Cell Cell;
    public readonly int Row;
    public readonly int Col;
    public readonly object OldValue;
    public readonly object NewValue;
    public readonly bool Accepted;

    public AcceptEditResult(bool accepted, int row, int col, Cell cell, object oldValue, object newValue)
    {
        Cell = cell;
        Accepted = accepted;
        Row = row;
        Col = col;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public static AcceptEditResult Reject(int row, int col)
    {
        return new AcceptEditResult(false, row, col, null, null, null);
    }
}