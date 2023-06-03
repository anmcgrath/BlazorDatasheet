namespace BlazorDatasheet.Formula.Core;

/// <summary>
/// Evaluated cell address - not relative.
/// </summary>
public class CellAddress
{
    public CellAddress(int row, int col)
    {
        Row = row;
        Col = col;
    }

    public int Row { get; }
    public int Col { get; }
}