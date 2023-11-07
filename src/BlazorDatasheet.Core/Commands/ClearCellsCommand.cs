using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Clears cell values in the given ranges
/// </summary>
public class ClearCellsCommand : IUndoableCommand
{
    private readonly IEnumerable<IRegion> _regions;
    private CellStoreRestoreData _restoreData;

    public ClearCellsCommand(SheetRange range) : this(new[] { range.Region })
    {
    }

    public ClearCellsCommand(IEnumerable<IRegion> regions)
    {
        _regions = regions.Select(x => x.Clone()).ToList();
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.ClearCellsImpl(_regions);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}