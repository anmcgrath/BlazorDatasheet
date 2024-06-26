using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

public class CopyRangeCommand : IUndoableCommand
{
    private SheetRange _fromRange;
    private SheetRange[] _toRanges;
    private readonly CopyOptions _copyOptions;

    private CellStoreRestoreData _cellStoreRestore;

    /// <summary>
    /// Copies data from one range to another. The from range must only have a single region.
    /// </summary>
    /// <param name="fromRange"></param>
    /// <param name="toRanges"></param>
    public CopyRangeCommand(SheetRange fromRange, SheetRange[] toRanges, CopyOptions copyOptions)
    {
        _fromRange = fromRange;
        _toRanges = toRanges;
        _copyOptions = copyOptions;
    }

    /// <summary>
    /// Copies data from one range to another. Only works if the range has a single region.
    /// </summary>
    /// <param name="fromRange"></param>
    /// <param name="toRange"></param>
    public CopyRangeCommand(SheetRange fromRange, SheetRange toRange, CopyOptions copyOptions)
    {
        _fromRange = fromRange;
        _copyOptions = copyOptions;
        _toRanges = new[] { toRange };
    }

    public bool Execute(Sheet sheet)
    {
        foreach (var range in _toRanges)
            Copy(_fromRange.Region, range.Region, sheet);

        return true;
    }

    private void Copy(IRegion fromRegion, IRegion toRegion, Sheet sheet)
    {
        _cellStoreRestore = sheet.Cells.CopyImpl(fromRegion, toRegion, _copyOptions);
    }

    public bool Undo(Sheet sheet)
    {
        foreach (var toRange in _toRanges)
        {
            sheet.Cells.ClearCellsImpl(new List<IRegion>() { toRange.Region });
            sheet.Cells.Restore(_cellStoreRestore);
        }

        return true;
    }
}