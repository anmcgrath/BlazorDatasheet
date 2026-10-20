using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Protection;

namespace BlazorDatasheet.Core.Commands.Tables;

/// <summary>
/// Removes a table from the sheet. See <see cref="TableCollection.Remove"/>.
/// </summary>
internal class RemoveTableCommand : BaseCommand, IUndoableCommand
{
    private readonly string _name;
    private TableStore.TableState? _removed;

    public RemoveTableCommand(string name)
    {
        _name = name;
    }

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(SheetOperation.Configure);

    protected override bool CanExecuteCore(Sheet sheet) => sheet.Tables.Contains(_name);

    protected override bool ExecuteCore(Sheet sheet)
    {
        _removed = sheet.Tables.RemoveCore(_name);
        if (_removed == null)
            return false;

        sheet.FormulaEngine.RefreshTableReferences(_removed.Name);
        sheet.Workbook.Tables.OnTableChanged(new TableChangedEventArgs(_removed.Name, TableChangeKind.Removed));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        // another table may have taken the name meanwhile, e.g. on another sheet
        if (_removed == null || sheet.Workbook.Tables.Contains(_removed.Name))
            return false;

        sheet.Tables.RestoreCore(_removed);
        sheet.FormulaEngine.RefreshTableReferences(_removed.Name);
        sheet.Workbook.Tables.OnTableChanged(new TableChangedEventArgs(_removed.Name, TableChangeKind.Added));
        return true;
    }
}
