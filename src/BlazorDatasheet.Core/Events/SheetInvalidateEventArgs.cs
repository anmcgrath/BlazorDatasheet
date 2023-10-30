using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

public class SheetInvalidateEventArgs
{
    public IReadOnlySet<CellPosition> DirtyCells { get; }

    internal SheetInvalidateEventArgs(IReadOnlySet<CellPosition> dirtyCells)
    {
        DirtyCells = dirtyCells;
    }
}