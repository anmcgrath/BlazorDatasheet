using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Events.Visual;

public class DirtySheetEventArgs
{
    public Range1DStore<bool> DirtyRows { get; init; } = default!;
}