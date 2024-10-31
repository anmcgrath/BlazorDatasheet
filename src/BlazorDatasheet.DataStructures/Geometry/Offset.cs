namespace BlazorDatasheet.DataStructures.Geometry;

public readonly record struct Offset
{
    public int Rows { get; }
    public int Columns { get; }

    public Offset(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
    }
}