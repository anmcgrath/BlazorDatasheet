namespace BlazorDatasheet.DataStructures.Geometry;

public readonly record struct CellPosition(int Row, int Col)
{
    public override string ToString()
    {
        return $"({Col}, {Row})";
    }
}