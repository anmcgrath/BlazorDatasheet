using BlazorDatasheet.Core.Commands.Filters;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Filter;

public class ColumnFilterCollection
{
    private readonly Sheet _sheet;
    internal readonly Range1DStore<List<IFilter>?> Store = new(null);

    /// <summary>
    /// The current rows that are filtered.
    /// </summary>
    internal List<Interval> FilteredRows = new();

    public ColumnFilterCollection(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Removes all filters from column <paramref name="columnIndex"/>
    /// </summary>
    /// <param name="columnIndex"></param>
    public void Clear(int columnIndex)
    {
        _sheet.Commands.ExecuteCommand(new ClearFiltersCommand(columnIndex));
    }

    /// <summary>
    /// Clears all filters from column <paramref name="columnIndex"/>
    /// </summary>
    /// <param name="columnIndex"></param>
    internal void ClearImpl(int columnIndex)
    {
        Store.Clear(columnIndex, 1);
    }

    /// <summary>
    /// Clears all filters
    /// </summary>
    internal void ClearImpl()
    {
        Store.Clear();
    }

    /// <summary>
    /// Applies all column filters to the datasheet.
    /// </summary>
    public void Apply()
    {
        var cmd = new ApplyColumnFiltersCommand();
        _sheet.Commands.ExecuteCommand(cmd);
    }


    /// <summary>
    /// Returns the filters on column <paramref name="columnIndex"/>
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <returns></returns>
    public ColumnFilter Get(int columnIndex)
    {
        return new ColumnFilter(columnIndex, Store.Get(columnIndex) ?? []);
    }

    /// <summary>
    /// Returns all filters.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ColumnFilter> GetAll()
    {
        return Store.GetAllIntervalData().Select((i, d) => new ColumnFilter(i.interval.Start, i.data ?? []));
    }


    internal void SetImpl(int columnIndex, List<IFilter> filters)
    {
        Store.Set(columnIndex, filters);
    }

    /// <summary>
    /// Sets the filters at column <paramref name="columnIndex"/>
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="filters"></param>
    public void Set(int columnIndex, List<IFilter> filters)
    {
        var cmd = new SetColumnFilterCommand(columnIndex, filters);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the filter at column <paramref name="columnIndex"/>
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="filter"></param>
    public void Set(int columnIndex, IFilter filter)
    {
        Set(columnIndex, [filter]);
    }
}