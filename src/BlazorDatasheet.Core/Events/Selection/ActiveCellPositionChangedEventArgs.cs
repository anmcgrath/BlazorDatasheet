using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Selection;

public class ActiveCellPositionChangedEventArgs
{
    public CellPosition PreviousPosition { get; }
    public CellPosition NewPosition { get; }

    public ActiveCellPositionChangedEventArgs(CellPosition previousPosition, CellPosition newPosition)
    {
        PreviousPosition = previousPosition;
        NewPosition = newPosition;
    }
}