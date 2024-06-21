namespace BlazorDatasheet.DataStructures.Graph;

public abstract class Vertex<T>
{
    public abstract string Key { get; }
    public T Data { get; }
}