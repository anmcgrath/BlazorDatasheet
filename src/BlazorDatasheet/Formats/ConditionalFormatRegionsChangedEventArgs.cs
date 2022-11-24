using BlazorDatasheet.Data;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatRegionsChangedEventArgs
{
    public IEnumerable<IRegion> RegionsRemoved { get; }
    public IEnumerable<IRegion> RegionsAdded { get; }

    public ConditionalFormatRegionsChangedEventArgs(IEnumerable<IRegion> regionsRemoved,
        IEnumerable<IRegion> regionsAdded)
    {
        RegionsRemoved = regionsRemoved;
        RegionsAdded = regionsAdded;
    }
}