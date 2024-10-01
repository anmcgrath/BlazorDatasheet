namespace BlazorDatasheet.Core.Data.Filter;

public class ColumnFilterStore
{
    private readonly Sheet _sheet;
    private readonly Dictionary<int, ColumnFilter> _filters = new();

    internal ColumnFilterStore(Sheet sheet)
    {
        _sheet = sheet;
    }

    public void Apply()
    {
        _sheet.Commands.BeginCommandGroup();
        _sheet.Rows.Unhide(0, _sheet.NumRows);

        var handler = new FilterHandler();
        var filterDict = _filters.ToDictionary(x => x.Key,
            x => new[] { x.Value.ValueFilter }.Concat(x.Value.CustomFilters.Filters).ToArray());

        var hiddenIntervals = handler.GetHiddenRows(_sheet, filterDict);

        foreach (var interval in hiddenIntervals)
        {
            _sheet.Rows.Hide(interval.Start, interval.End - interval.Start + 1);
        }

        _sheet.Commands.EndCommandGroup();
    }

    public ColumnFilter? GetFilter(int column)
    {
        return _filters.GetValueOrDefault(column);
    }

    public void SetFilter(int column, ColumnFilter filter)
    {
        if (!_filters.TryAdd(column, filter))
            _filters[column] = filter;
    }
}