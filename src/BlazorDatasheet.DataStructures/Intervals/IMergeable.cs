namespace BlazorDatasheet.DataStructures.Intervals;

public interface IMergeable<T>
{
    void Merge(T item);
    T Clone();
}