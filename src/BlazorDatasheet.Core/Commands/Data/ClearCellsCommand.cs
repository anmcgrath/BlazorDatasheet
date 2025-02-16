using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Clears cell values in the given ranges
/// </summary>
public class ClearCellsCommand : BaseCommand, IUndoableCommand
{
    private readonly IEnumerable<IRegion> _regions;
    private CellStoreRestoreData _restoreData = null!;

    public ClearCellsCommand(IEnumerable<IRegion> regions)
    {
        _regions = regions.Select(x => x.Clone()).ToList();
    }

    public ClearCellsCommand(IRegion region)
    {
        _regions = new List<IRegion> { region.Clone() };
    }

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.ClearCellsImpl(_regions);
        return true;
    }

    public override bool CanExecute(Sheet sheet)
    {
        foreach (var region in _regions)
        {
            if (sheet.Cells.ContainsReadOnly(region))
                return false;
        }

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}