using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Graph;

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