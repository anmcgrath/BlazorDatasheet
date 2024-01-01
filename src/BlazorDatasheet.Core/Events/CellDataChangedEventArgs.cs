using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

public class CellDataChangedEventArgs
{
    public IEnumerable<IRegion> Regions { get; }
    public IEnumerable<CellPosition> Positions { get; }

    public CellDataChangedEventArgs(IEnumerable<IRegion> regions, IEnumerable<CellPosition> positions)
    {
        Regions = regions;
        Positions = positions;
    }
}