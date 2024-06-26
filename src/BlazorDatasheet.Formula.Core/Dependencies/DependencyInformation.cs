namespace BlazorDatasheet.Formula.Core.Dependencies;

public class DependencyInformation
{
    public List<RegionDependency> Prec { get; }
    public List<RegionDependency> Adj { get; }
    public List<RegionDependency> RegionDependencies { get; }

    internal DependencyInformation(List<RegionDependency> prec, List<RegionDependency> adj,
        List<RegionDependency> regionDependencies)
    {
        Prec = prec;
        Adj = adj;
        RegionDependencies = regionDependencies;
    }
}