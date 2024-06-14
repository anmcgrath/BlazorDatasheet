using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public class RegionRestoreData<T>
{
    public List<DataRegion<T>> RegionsRemoved { get; init; } = new();
    public List<DataRegion<T>> RegionsAdded { get; init; } = new();

    public List<AppliedShift> Shifts { get; init; } = new();

    public void Merge(RegionRestoreData<T> item)
    {
        RegionsAdded.AddRange(item.RegionsAdded);
        RegionsRemoved.AddRange(item.RegionsRemoved);
        Shifts.AddRange(item.Shifts);
    }
}

public record struct AppliedShift(Axis Axis, int Index, int Amount)
{
}