using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class CopyRangeCommand : IUndoableCommand
{
    private SheetRange _fromRange;
    private SheetRange _toRange;

    private CellStoreRestoreData _cellStoreRestore;

    /// <summary>
    /// Copies data from one range to another. Only works if the range has a single region.
    /// </summary>
    /// <param name="fromRange"></param>
    /// <param name="toRange"></param>
    public CopyRangeCommand(SheetRange fromRange, SheetRange toRange)
    {
        _fromRange = fromRange;
        _toRange = toRange;
    }

    public bool Execute(Sheet sheet)
    {
        if (_fromRange.Regions.Count != 1)
            return false;

        foreach (var region in _toRange.Regions)
            Copy(_fromRange.Regions.First(), region, sheet);
        
        sheet.MarkDirty(_toRange.Regions);

        return true;
    }

    private void Copy(IRegion fromRegion, IRegion toRegion, Sheet sheet)
    {
        _cellStoreRestore = sheet.Cells.CopyImpl(fromRegion, toRegion);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.ClearCellsImpl(_toRange.Regions);
        sheet.Cells.Restore(_cellStoreRestore);
        return true;
    }
}