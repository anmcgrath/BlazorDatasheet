using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class FilterGroup : IFilter
{
    public List<IFilter> Filters { get; private set; } = new();
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
    public IFilter Clone()
    {
        return new FilterGroup()
        {
            Filters = Filters.Select(x => x.Clone()).ToList(),
            JoinType = JoinType
        };
    }
}