using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

public class CopyRangeCommand : BaseCommand, IUndoableCommand
{
    private readonly SheetRange _fromRange;
    private readonly SheetRange[] _toRanges;
    private readonly CopyOptions _copyOptions;

    private readonly List<CellStoreRestoreData> _restoreData = new();

    /// <summary>
    /// Copies data from one range to another. The from range must only have a single region.
    /// </summary>
    /// <param name="fromRange"></param>
    /// <param name="toRanges"></param>
    /// <param name="copyOptions"></param>
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
    /// <param name="copyOptions"></param>
    public CopyRangeCommand(SheetRange fromRange, SheetRange toRange, CopyOptions copyOptions)
    {
        _fromRange = fromRange;
        _copyOptions = copyOptions;
        _toRanges = new[] { toRange };
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        _restoreData.Clear();
        foreach (var range in _toRanges)
            Copy(_fromRange.Region, range.Region, sheet);

        return true;
    }

    public override bool CanExecuteProtected(Sheet sheet) => _toRanges.All(x =>
        ((!_copyOptions.CopyValues && !_copyOptions.CopyFormula) ||
         sheet.Protection.CanEdit(GetAffectedRegion(sheet, x.Region))) &&
        (!_copyOptions.CopyFormat || sheet.Protection.CanFormat(GetAffectedRegion(sheet, x.Region))));

    private void Copy(IRegion fromRegion, IRegion toRegion, Sheet sheet)
    {
        _restoreData.Add(sheet.Cells.CopyImpl(fromRegion, GetAffectedRegion(sheet, toRegion), _copyOptions));
    }

    private IRegion GetAffectedRegion(Sheet sheet, IRegion target)
    {
        var source = _fromRange.Region.GetIntersection(sheet.Region);
        if (source == null)
            return target;
        return new Region(target.Top, Math.Max(target.Bottom, target.Top + source.Height - 1),
            target.Left, Math.Max(target.Right, target.Left + source.Width - 1));
    }

    public bool Undo(Sheet sheet)
    {
        for (var i = _restoreData.Count - 1; i >= 0; i--)
        {
            if (_copyOptions.CopyValues || _copyOptions.CopyFormula)
                sheet.Cells.ClearCellsImpl(new[] { GetAffectedRegion(sheet, _toRanges[i].Region) });
            sheet.Cells.Restore(_restoreData[i]);
        }

        return true;
    }
}