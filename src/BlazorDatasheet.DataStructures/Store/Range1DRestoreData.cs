using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.DataStructures.Store;

public class Range1DRestoreData<T>
{
    public List<(Interval, T value)> RemovedData { get; } = new();
    public List<(Interval, T value)> AddedData { get; } = new();

    internal Range1DRestoreData()
    {
    }
}