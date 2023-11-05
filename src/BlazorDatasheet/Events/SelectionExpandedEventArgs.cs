using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Events;

public class SelectionExpandedEventArgs
{
    public IRegion Original { get; }
    public IRegion Expanded { get; }

    public SelectionExpandedEventArgs(IRegion original, IRegion expanded)
    {
        Original = original;
        Expanded = expanded;
    }
}