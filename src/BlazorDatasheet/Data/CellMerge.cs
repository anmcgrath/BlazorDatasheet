using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.Data;

/// <summary>
/// Specifies a set of merged cells
/// </summary>
public class CellMerge : ISpatialData
{
    public IRegion Region { get; }

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