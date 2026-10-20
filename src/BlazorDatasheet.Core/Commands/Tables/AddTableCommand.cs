using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Tables;

/// <summary>
/// Adds a region table or a sheet table to the sheet. See <see cref="TableCollection.Add"/>.
/// </summary>
internal class AddTableCommand : BaseCommand, IUndoableCommand
{
    private readonly string _name;
    private readonly IRegion? _region;
    private readonly bool _showTotals;
    private readonly bool _isSheetTable;

    // kept so that redo puts back the same table, which restore data held by other commands refers to
    private long? _id;

    public AddTableCommand(string name, IRegion? region, bool showTotals, bool isSheetTable)
    {
        _name = name;
        _region = region?.Clone();
        _showTotals = showTotals;
        _isSheetTable = isSheetTable;
    }

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(SheetOperation.Configure);

    protected override bool CanExecuteCore(Sheet sheet) =>
        sheet.Workbook.Tables.CanAdd(sheet, _name, _region, _isSheetTable, _showTotals, out _);

    protected override bool ExecuteCore(Sheet sheet)
    {
        var tables = sheet.Workbook.Tables;
        if (!tables.CanAdd(sheet, _name, _region, _isSheetTable, _showTotals, out _))
            return false;

        _id = sheet.Tables.AddCore(_name, _region, _showTotals, _isSheetTable, _id);
        sheet.FormulaEngine.RefreshTableReferences(_name);
        tables.OnTableChanged(new TableChangedEventArgs(_name, TableChangeKind.Added));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (sheet.Tables.RemoveCore(_name) == null)
            return false;

        sheet.FormulaEngine.RefreshTableReferences(_name);
        sheet.Workbook.Tables.OnTableChanged(new TableChangedEventArgs(_name, TableChangeKind.Removed));
        return true;
    }
}
