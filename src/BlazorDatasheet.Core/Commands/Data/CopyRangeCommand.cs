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
    /// Copies data from one range to each of the target ranges. The from range must only have a single region.
    /// The source may be on another sheet of the same workbook; the command is executed on the target sheet.
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
        using var updates = sheet.SuspendUpdates();
        foreach (var range in _toRanges)
            _restoreData.Add(sheet.Cells.CopyImpl(_fromRange, GetAffectedRegion(range.Region), _copyOptions));

        return true;
    }

    /// <summary>
    /// A block can't land across the edge of a merged cell, as in Excel: the target must hold every merge it
    /// touches, or the paste would break the merge up.
    /// </summary>
    protected override bool CanExecuteCore(Sheet sheet) => _toRanges.All(x =>
    {
        var target = GetAffectedRegion(x.Region);
        return sheet.Cells.GetMerges(target).All(target.Contains);
    });

    public override bool CanExecuteProtected(Sheet sheet) => _toRanges.All(x =>
        ((!_copyOptions.CopyValues && !_copyOptions.CopyFormula) ||
         sheet.Protection.CanEdit(GetAffectedRegion(x.Region))) &&
        ((!_copyOptions.CopyFormat && !CopiesAnyMerges()) ||
         sheet.Protection.CanFormat(GetAffectedRegion(x.Region))));

    private bool CopiesAnyMerges()
    {
        if (!_copyOptions.CopyMerges)
            return false;
        var source = SourceRegion;
        return source != null && _fromRange.Sheet.Cells.AnyMerges(source);
    }

    private IRegion? SourceRegion => _fromRange.Region.GetIntersection(_fromRange.Sheet.Region);

    /// <summary>
    /// The region written at the target: at least the size of the source, from the target's top-left.
    /// </summary>
    private IRegion GetAffectedRegion(IRegion target)
    {
        var source = SourceRegion;
        if (source == null)
            return target;
        return new Region(target.Top, Math.Max(target.Bottom, target.Top + source.Height - 1),
            target.Left, Math.Max(target.Right, target.Left + source.Width - 1));
    }

    public bool Undo(Sheet sheet)
    {
        using var updates = sheet.SuspendUpdates();
        for (var i = _restoreData.Count - 1; i >= 0; i--)
        {
            if (_copyOptions.CopyValues || _copyOptions.CopyFormula)
                sheet.Cells.ClearCellsImpl(new[] { GetAffectedRegion(_toRanges[i].Region) });
            sheet.Cells.Restore(_restoreData[i]);
        }

        return true;
    }

    public int RetainedCellCount
    {
        get
        {
            var count = 0;
            foreach (var restoreData in _restoreData)
                count += restoreData.RetainedCellCount;
            return count;
        }
    }
}
