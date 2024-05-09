using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class SortRangeCommand : IUndoableCommand
{
    private IRegion _region;
    private List<ColumnSortOptions>? _sortOptions = new List<ColumnSortOptions>();

    /// <summary>
    /// Sorts the specified region on values using the specified sort options.
    /// </summary>
    /// <param name="region">The region to sort</param>
    /// <param name="sortOptions">The column sort options, if null the default sort (sort on column 0 ascending) will be used.</param>
    public SortRangeCommand(IRegion region, List<ColumnSortOptions>? sortOptions = null)
    {
        _region = region;
        _sortOptions = sortOptions ?? new List<ColumnSortOptions>()
            { new(0, true) };
    }

    public bool Execute(Sheet sheet)
    {
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        return true;
    }
}

public class ColumnSortOptions
{
    /// <summary>
    /// The column index, relative to the range being sorted.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Whether to sort in ascending order.
    /// </summary>
    public bool Ascending { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="ascending"></param>
    public ColumnSortOptions(int columnIndex, bool ascending)
    {
        ColumnIndex = columnIndex;
        Ascending = ascending;
    }
}