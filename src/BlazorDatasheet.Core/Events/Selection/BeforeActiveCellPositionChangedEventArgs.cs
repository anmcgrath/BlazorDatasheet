using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Selection;

public class BeforeActiveCellPositionChangedEventArgs
{
    public CellPosition OldActivePosition { get; }
    public CellPosition NewCellPosition { get; set; }

    public BeforeActiveCellPositionChangedEventArgs(CellPosition oldActivePosition, CellPosition newCellPosition)
    {
        OldActivePosition = oldActivePosition;
        NewCellPosition = newCellPosition;
    }
}