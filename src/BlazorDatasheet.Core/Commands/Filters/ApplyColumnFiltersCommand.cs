using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Filters;

public class ApplyColumnFiltersCommand : IUndoableCommand
{
    private IUndoableCommand _commandRun = null!;
    public bool CanExecute(Sheet sheet) => true;

    public bool Execute(Sheet sheet)
    {
        // 1. Un-hide all rows filtered by the current column filters
        var unHideExistingCommand = new UnhideCommand(sheet.Columns.Filters.FilteredRows, Axis.Row);
        var columnFilters = sheet.Columns.Filters.GetAll();
        var handler = new FilterHandler();
        var hiddenRows = handler.GetHiddenRows(sheet, columnFilters);
        var hideCommand = new HideCommand(hiddenRows, Axis.Row);

        _commandRun = new CommandGroup(unHideExistingCommand, hideCommand);
        _commandRun.Execute(sheet);

        sheet.Columns.Filters.FilteredRows = hiddenRows;
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        return _commandRun.Undo(sheet);
    }
}