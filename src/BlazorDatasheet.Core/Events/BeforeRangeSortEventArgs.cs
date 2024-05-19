using System.ComponentModel;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

public class BeforeRangeSortEventArgs : CancelEventArgs
{
    public IRegion Region { get; }
    public IList<ColumnSortOptions>? SortOptions { get; }

    public BeforeRangeSortEventArgs(IRegion region, IList<ColumnSortOptions>? sortOptions)
    {
        Region = region;
        SortOptions = sortOptions;
    }
}