using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class FilterGroup : IFilter
{
    public List<IFilter> Filters { get; } = new();
    public FilterGroupJoin JoinType { get; set; }

    public bool Match(CellValue cellValue)
    {
        return JoinType switch
        {
            FilterGroupJoin.And => Filters.All(f => f.Match(cellValue)),
            FilterGroupJoin.Or => Filters.Any(f => f.Match(cellValue)),
            _ => false
        };
    }

    public bool IncludeBlanks => Filters.TrueForAll(x => x.IncludeBlanks);
}