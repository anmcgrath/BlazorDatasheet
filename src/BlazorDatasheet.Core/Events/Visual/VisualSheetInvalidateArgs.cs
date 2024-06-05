using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Visual;

public class VisualSheetInvalidateArgs
{
    public HashSet<int> DirtyRows { get; }

    public VisualSheetInvalidateArgs(HashSet<int> dirtyRows)
    {
        DirtyRows = dirtyRows;
    }
}