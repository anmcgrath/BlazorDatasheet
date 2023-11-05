namespace BlazorDatasheet.DataStructures.Geometry;

public readonly record struct CellPosition(int row, int col)
{
    public override string ToString()
    {
        return $"({row}, {col})";
    }
}