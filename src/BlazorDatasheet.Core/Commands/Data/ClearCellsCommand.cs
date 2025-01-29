using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Clears cell values in the given ranges
/// </summary>
public class ClearCellsCommand : BaseCommand, IUndoableCommand
{
    public IEnumerable<IRegion> Regions { get; }
    private CellStoreRestoreData _restoreData = null!;

    public ClearCellsCommand(SheetRange range) : this([range.Region])
    {
    }

    public ClearCellsCommand(IEnumerable<IRegion> regions)
    {
        Regions = regions.Select(x => x.Clone()).ToList();
    }

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.ClearCellsImpl(Regions);
        return true;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}