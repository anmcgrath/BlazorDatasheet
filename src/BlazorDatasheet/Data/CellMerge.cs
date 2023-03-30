using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.Data;

public class CellMerge : ISpatialData
{
    public IRegion Region { get; }

    /// <summary>
    /// Describes a merged region of cells.
    /// </summary>
    /// <param name="region">The region of cells included in this merge.</param>
    public CellMerge(IRegion region)
    {
        Region = region;
        _envelope = new Envelope(region.TopLeft.Col, 
                                 region.TopLeft.Row, 
                                 region.BottomRight.Col,
                                 region.BottomRight.Row);
    }

    private Envelope _envelope;
    public ref readonly Envelope Envelope => ref _envelope;
}