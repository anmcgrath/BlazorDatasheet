using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Protection;

namespace BlazorDatasheet.Core.Commands.Tables;

/// <summary>
/// Shows or hides the totals row of a region table. See <see cref="TableCollection.SetShowTotals"/>.
/// </summary>
internal class SetTableShowTotalsCommand : BaseCommand, IUndoableCommand
{
    private readonly string _name;
    private readonly bool _showTotals;
    private TableStore.TableState? _before;

    public SetTableShowTotalsCommand(string name, bool showTotals)
    {
        _name = name;
        _showTotals = showTotals;
    }

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(SheetOperation.Configure);

    protected override bool CanExecuteCore(Sheet sheet) =>
        sheet.Workbook.Tables.CanSetShowTotals(sheet, _name, _showTotals, out _);

    protected override bool ExecuteCore(Sheet sheet)
    {
        if (!sheet.Workbook.Tables.CanSetShowTotals(sheet, _name, _showTotals, out _))
            return false;

        _before = sheet.Tables.GetState(_name);
        if (_before == null || _before.IsSheetTable || !sheet.Tables.SetShowTotalsCore(_name, _showTotals))
            return false;

        sheet.FormulaEngine.RefreshTableReferences(_before.Name);
        sheet.Workbook.Tables.OnTableChanged(
            new TableChangedEventArgs(_before.Name, TableChangeKind.ShowTotalsChanged));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_before == null || !sheet.Tables.SetShowTotalsCore(_before.Name, _before.ShowTotals))
            return false;

        sheet.FormulaEngine.RefreshTableReferences(_before.Name);
        sheet.Workbook.Tables.OnTableChanged(
            new TableChangedEventArgs(_before.Name, TableChangeKind.ShowTotalsChanged));
        return true;
    }
}
