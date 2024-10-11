using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;

namespace BlazorDatasheet.Core.Commands.Filters;

public class ClearFiltersCommand : IUndoableCommand
{
    private readonly int _columnIndex;
    private readonly bool _clearAllFilters;
    private List<ColumnFilter>? _previousFilters;
    private IUndoableCommand _applyCommand = null!;

    /// <summary>
    /// Create a clear filter command that clears the colum filter at <paramref name="columnIndex"/>
    /// </summary>
    /// <param name="columnIndex"></param>
    public ClearFiltersCommand(int columnIndex)
    {
        _columnIndex = columnIndex;
        _clearAllFilters = false;
    }

    /// <summary>
    /// Create a clear filter command that clears all column filters.
    /// </summary>
    public ClearFiltersCommand()
    {
        _clearAllFilters = true;
    }

    public bool Execute(Sheet sheet)
    {
        if (_clearAllFilters)
            _previousFilters = sheet.Columns.Filters.GetAll().ToList();
        else
            _previousFilters = [sheet.Columns.Filters.Get(_columnIndex)];

        if (_clearAllFilters)
            sheet.Columns.Filters.ClearImpl();
        else
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
        {
            foreach (var columnFilter in _previousFilters)
                sheet.Columns.Filters.SetImpl(columnFilter.Column, columnFilter.Filters.ToList());
        }

        return true;
    }
}