using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public class RegionRestoreData<T>
{
    public List<DataRegion<T>> RegionsRemoved { get; init; } = new();
    public List<DataRegion<T>> RegionsAdded { get; init; } = new();
    public List<AppliedShift>? Shifts { get; set; } = null;

    public RegionRestoreData<T> Merge(RegionRestoreData<T> item)
    {
        RegionsAdded.AddRange(item.RegionsAdded);
        RegionsRemoved.AddRange(item.RegionsRemoved);

        if (item.Shifts != null)
        {
            if (Shifts == null)
                Shifts = new List<AppliedShift>(item.Shifts);
            else
                Shifts.AddRange(item.Shifts);
        }

        return this;
    }
}

public record struct AppliedShift(Axis Axis, int Index, int Amount, string? SheetName)
{
}