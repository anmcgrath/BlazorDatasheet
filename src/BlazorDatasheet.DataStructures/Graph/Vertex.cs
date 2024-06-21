namespace BlazorDatasheet.DataStructures.Graph;

public abstract class Vertex
{
    public abstract string Key { get; }
}

public abstract class Vertex<TData> : Vertex
{
    public abstract TData Data { get; }
}