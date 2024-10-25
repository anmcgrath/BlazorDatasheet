using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Selecting;

internal class SelectionSnapshot
{
    public IRegion? ActiveRegion { get; }
    public IReadOnlyList<IRegion> Regions { get; }
    public CellPosition ActiveCellPosition { get; private set; }

    public SelectionSnapshot(IRegion? activeRegion, IReadOnlyList<IRegion> regions, CellPosition activeCellPosition)
    {
        ActiveRegion = activeRegion;
        Regions = regions;
        ActiveCellPosition = activeCellPosition;
    }
}