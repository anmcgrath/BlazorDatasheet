namespace BlazorDatasheet.Data;

public struct CellPosition
{
    public int Row { get; set; }
    public int Col { get; set; }
    public bool InvalidPosition { get; } = false;

    public CellPosition(int row, int col)
    {
        Row = row;
        Col = col;
        if (row < 0 || col < 0)
            InvalidPosition = true;
    }

    public bool Equals(int row, int col)
    {
        return Row == row && Col == col;
    }

    public override string ToString()
    {
        return $"({Col}, {Row})";
    }
}