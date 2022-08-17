namespace BlazorDatasheet.Model;

public class CellPosition
{
    public int Row { get; set; }
    public int Col { get; set; }

    public bool Equals(int row, int col)
    {
        return Row == row && Col == col;
    }
}