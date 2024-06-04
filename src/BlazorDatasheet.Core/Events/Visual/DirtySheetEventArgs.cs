using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Events.Visual;

public class DirtySheetEventArgs
{
    public ConsolidatedDataStore<bool> DirtyRegions { get; init; } = default!;
}