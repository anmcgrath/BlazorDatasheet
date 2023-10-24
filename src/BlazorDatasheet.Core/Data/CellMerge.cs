using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.Core.Data;

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
        _envelope = region.ToEnvelope();
    }

    private Envelope _envelope;
    public ref readonly Envelope Envelope => ref _envelope;
}