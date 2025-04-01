using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Visual;

public class FormatChangedEventArgs
{
    public IRegion Region { get; }
    public IReadonlyCellFormat NewFormat { get; }

    public FormatChangedEventArgs(IRegion region, IReadonlyCellFormat newFormat)
    {
        Region = region;
        NewFormat = newFormat;
    }
}