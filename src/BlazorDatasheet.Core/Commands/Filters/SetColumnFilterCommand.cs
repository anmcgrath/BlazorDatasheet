using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;

namespace BlazorDatasheet.Core.Commands.Filters;

public class SetColumnFilterCommand : IUndoableCommand
{
    private readonly int _columnIndex;
    private readonly List<IFilter> _filters;
    private List<IFilter>? _previousFilters;
    private IUndoableCommand _applyCommand = null!;

    public SetColumnFilterCommand(int columnIndex, List<IFilter> filters)
    {
        _columnIndex = columnIndex;
        _filters = filters;
    }

    public SetColumnFilterCommand(int columnIndex, IFilter filter) : this(columnIndex, [filter])
    {
    }

    public bool CanExecute(Sheet sheet) => sheet.Region.SpansCol(_columnIndex);
    public bool Execute(Sheet sheet)
    {
        _previousFilters = sheet.Columns.Filters.Get(_columnIndex).Filters.ToList();
        sheet.Columns.Filters.SetImpl(_columnIndex, _filters);
        _applyCommand = new ApplyColumnFiltersCommand();
        return _applyCommand.Execute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        if (_previousFilters == null || !_previousFilters.Any())
            sheet.Columns.Filters.ClearImpl(_columnIndex);
        else
            sheet.Columns.Filters.SetImpl(_columnIndex, _previousFilters);

        return _applyCommand.Undo(sheet);
    }
}