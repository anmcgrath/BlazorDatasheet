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
            Copy(_fromRange.Regions.First(), region.TopLeft, sheet);

        return true;
    }

    private void Copy(IRegion fromRegion, CellPosition toPosition, Sheet sheet)
    {
        _cellStoreRestore = sheet.Cells.Copy(fromRegion, toPosition);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_cellStoreRestore);
        return true;
    }
}