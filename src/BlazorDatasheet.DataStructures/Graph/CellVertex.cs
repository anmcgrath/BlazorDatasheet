namespace BlazorDatasheet.DataStructures.Graph;

public class CellVertex : Vertex
{
    public CellVertex(int row, int col)
    {
        Row = row;
        Col = col;
        Key = row + "," + col;
    }

    public int Row { get; }
    public int Col { get; }
    public override string Key { get; }
}