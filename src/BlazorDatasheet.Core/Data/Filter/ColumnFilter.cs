namespace BlazorDatasheet.Core.Data.Filter;

public class ColumnFilter
{
    public int Column { get; }
    public IReadOnlyList<IFilter> Filters { get; }

    public ColumnFilter(int column, IReadOnlyList<IFilter> filters)
    {
        Column = column;
        Filters = filters;
    }

    public ColumnFilter(int column, IFilter filter) : this(column, [filter])
    {
        
    }
}