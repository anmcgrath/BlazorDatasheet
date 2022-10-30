namespace BlazorDatasheet.Data.SpatialDataStructures;

public interface IMergeable<T>
{
    void Merge(T item);
    T Clone();
}