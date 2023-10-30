using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Visual;

public class VisualSheetInvalidateArgs
{
    public HashSet<CellPosition> DirtyCells { get; }

    public VisualSheetInvalidateArgs(HashSet<CellPosition> dirtyCells)
    {
        DirtyCells = dirtyCells;
    }
}