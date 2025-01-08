namespace BlazorDatasheet.DataStructures.Geometry;

public struct CellData<T>
{
    public T Data { get; }
    public CellPosition Position { get; }

    public CellData(T data, CellPosition position)
    {
        Data = data;
        Position = position;
    }
}