using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;

namespace BlazorDatasheet.Core.Commands.Filters;

public class ClearFiltersCommand : IUndoableCommand
{
    private readonly int _columnIndex;
    private List<IFilter>? _previousFilters;
    private IUndoableCommand _applyCommand = null!;

    public ClearFiltersCommand(int columnIndex)
    {
        _columnIndex = columnIndex;
    }

    public bool Execute(Sheet sheet)
    {
        _previousFilters = sheet.Columns.Filters.Get(_columnIndex).Filters.ToList();
        sheet.Columns.Filters.ClearImpl(_columnIndex);

        // Apply all filters to the sheet
        _applyCommand = new ApplyColumnFiltersCommand();
        _applyCommand.Execute(sheet);

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        _applyCommand.Undo(sheet);

        if (_previousFilters != null)
            sheet.Columns.Filters.SetImpl(_columnIndex, _previousFilters);
        return true;
    }
}