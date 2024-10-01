using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

/// <summary>
/// A column filter has one value filter and a list of custom filters.
/// </summary>
public class ColumnFilter
{
    public FilterGroup CustomFilters { get; } = new();
    public ValueFilter ValueFilter { get; } = new ValueFilter(new List<CellValue>());
}