using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Dependencies;

public class RegionDependency
{
    public IRegion From { get; }
    public IRegion To { get; }
    
    public string Color { get; }

    internal RegionDependency(IRegion from, IRegion to, string color)
    {
        From = from;
        To = to;
        Color = color;
    }
}