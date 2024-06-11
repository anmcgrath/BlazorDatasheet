using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public class RegionRestoreData<T>
{
    public List<DataRegion<T>> RegionsRemoved { get; init; } = new();
    public List<DataRegion<T>> RegionsAdded { get; init; } = new();

    public List<(Axis axis, int start, int shift)> Shifts { get; init; } = new();
    public List<(Edge edge, DataRegion<T> region, int amount)> Contractions { get; init; } = new();
    public void Merge(RegionRestoreData<T> item)
    {
        RegionsAdded.AddRange(item.RegionsAdded);
        RegionsRemoved.AddRange(item.RegionsRemoved);
        Shifts.AddRange(item.Shifts);
        Contractions.AddRange(item.Contractions);
    }
}