using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class FilterGroup : IColumnFilter
{
    public List<IColumnFilter> Filters { get; set; }
    public FilterGroupJoin JoinType { get; set; }

    public FilterGroup(List<IColumnFilter> filters)
    {
        Filters = filters;
    }

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