using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;

namespace BlazorDatasheet.Core.FormulaEngine;

public class RegionVertex : Vertex
{
    public RegionVertex(IRegion region, string regionName)
    {
        Region = region;
        Key = regionName;
    }

    public override string Key { get; }

    public IRegion Region { get; }
}