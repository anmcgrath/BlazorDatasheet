using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Selecting;

internal class SelectionSnapshot
{
    public int ActiveRegionIndex { get; }
    public IReadOnlyList<IRegion> Regions { get; }
    public CellPosition ActiveCellPosition { get; private set; }

    public SelectionSnapshot(int activeRegionIndex, IReadOnlyList<IRegion> regions, CellPosition activeCellPosition)
    {
        ActiveRegionIndex = activeRegionIndex;
        Regions = regions;
        ActiveCellPosition = activeCellPosition;
    }
}