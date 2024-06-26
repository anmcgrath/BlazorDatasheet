using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Dependencies;

public class DependencyInfo
{
    public IRegion From { get; }
    public IRegion To { get; }

    public DependencyType Type { get; }

    internal DependencyInfo(IRegion from, IRegion to, DependencyType type)
    {
        From = from;
        To = to;
        Type = type;
    }
}

public enum DependencyType
{
    Region,
    CalculationOrder
}