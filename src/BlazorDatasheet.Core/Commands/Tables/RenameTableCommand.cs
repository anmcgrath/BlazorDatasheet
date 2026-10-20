using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Protection;

namespace BlazorDatasheet.Core.Commands.Tables;

/// <summary>
/// Renames a table on the sheet. See <see cref="TableCollection.Rename"/>.
/// </summary>
internal class RenameTableCommand : BaseCommand, IUndoableCommand
{
    private readonly string _oldName;
    private readonly string _newName;

    // the name as the table had it, which may differ in case from the name given
    private string? _previousName;

    public RenameTableCommand(string oldName, string newName)
    {
        _oldName = oldName;
        _newName = newName;
    }

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(SheetOperation.Configure);

    protected override bool CanExecuteCore(Sheet sheet) =>
        sheet.Tables.Contains(_oldName) && sheet.Workbook.Tables.IsValidTableName(_newName, _oldName, out _);

    protected override bool ExecuteCore(Sheet sheet)
    {
        var table = sheet.Tables.Get(_oldName);
        if (table == null || !sheet.Workbook.Tables.IsValidTableName(_newName, _oldName, out _))
            return false;

        _previousName = table.Name;
        sheet.Tables.RenameCore(_previousName, _newName);
        sheet.FormulaEngine.RenameTable(_previousName, _newName);
        sheet.Workbook.Tables.OnTableChanged(
            new TableChangedEventArgs(_newName, TableChangeKind.Renamed, _previousName));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_previousName == null ||
            !sheet.Workbook.Tables.IsValidTableName(_previousName, _newName, out _) ||
            !sheet.Tables.RenameCore(_newName, _previousName))
            return false;

        sheet.FormulaEngine.RenameTable(_newName, _previousName);
        sheet.Workbook.Tables.OnTableChanged(
            new TableChangedEventArgs(_previousName, TableChangeKind.Renamed, _newName));
        return true;
    }
}
