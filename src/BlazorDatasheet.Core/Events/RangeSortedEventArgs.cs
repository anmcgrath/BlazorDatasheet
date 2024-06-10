using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

public class RangeSortedEventArgs
{
    /// <summary>
    /// The region that was requested to be sorted.
    /// </summary>
    public IRegion? Region { get; }

    /// <summary>
    /// The region that was sorted.
    /// </summary>
    public IRegion? SortedRegion { get; }

    /// <summary>
    /// Indices of the rows within sortedRegion before sorting.
    /// </summary>
    public IList<int> OldIndicies { get; set; }

    public RangeSortedEventArgs(IRegion? region, IRegion? sortedRegion, IList<int> oldIndicies)
    {
        Region = region;
        SortedRegion = sortedRegion;
        OldIndicies = oldIndicies;
    }
}