using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Tables;

/// <summary>
/// Moves or resizes a region table. See <see cref="TableCollection.Resize"/>.
/// </summary>
internal class ResizeTableCommand : BaseCommand, IUndoableCommand
{
    private readonly string _name;
    private readonly IRegion _region;
    private TableStore.TableState? _before;

    public ResizeTableCommand(string name, IRegion region)
    {
        _name = name;
        _region = region.Clone();
    }

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(SheetOperation.Configure);

    protected override bool CanExecuteCore(Sheet sheet) =>
        sheet.Workbook.Tables.CanResize(sheet, _name, _region, out _);

    protected override bool ExecuteCore(Sheet sheet)
    {
        if (!sheet.Workbook.Tables.CanResize(sheet, _name, _region, out _))
            return false;

        _before = sheet.Tables.GetState(_name);
        if (_before == null || !sheet.Tables.SetRegionCore(_name, _region))
            return false;

        // a different header row has different names, which are not renames
        sheet.Tables.RefreshColumnNames(_name);
        sheet.FormulaEngine.RefreshTableReferences(_before.Name);
        sheet.Workbook.Tables.OnTableChanged(new TableChangedEventArgs(_before.Name, TableChangeKind.Resized));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_before?.Region == null || !sheet.Tables.SetRegionCore(_before.Name, _before.Region, _before.IsDeleted))
            return false;

        sheet.Tables.RefreshColumnNames(_before.Name);
        sheet.FormulaEngine.RefreshTableReferences(_before.Name);
        sheet.Workbook.Tables.OnTableChanged(new TableChangedEventArgs(_before.Name, TableChangeKind.Resized));
        return true;
    }
}
