using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Filters;

public class ApplyColumnFiltersCommand : BaseCommand, IUndoableCommand
{
    private RowColInfoRestoreData _unhideRestore = null!;
    private RowColInfoRestoreData _hideRestore = null!;
    private List<BlazorDatasheet.DataStructures.Intervals.Interval> _previousFilteredRows = new();
    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(SheetOperation.Filter);

    protected override bool ExecuteCore(Sheet sheet)
    {
        var columnFilters = sheet.Columns.Filters.GetAll();
        var hiddenRows = new FilterHandler().GetHiddenRows(sheet, columnFilters);
        _previousFilteredRows = sheet.Columns.Filters.FilteredRows;
        _unhideRestore = sheet.Rows.UnhideImpl(_previousFilteredRows);
        _hideRestore = sheet.Rows.HideImpl(hiddenRows);
        sheet.Columns.Filters.FilteredRows = hiddenRows;
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Rows.Restore(_hideRestore);
        sheet.Rows.Restore(_unhideRestore);
        sheet.Columns.Filters.FilteredRows = _previousFilteredRows;
        return true;
    }
}