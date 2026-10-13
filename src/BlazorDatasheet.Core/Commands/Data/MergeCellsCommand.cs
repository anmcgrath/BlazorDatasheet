using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

public class MergeCellsCommand : BaseCommand, IUndoableCommand
{
    private readonly List<IRegion> _overridenMergedRegions = new();
    private readonly List<IRegion> _mergesPerformed = new();
    private CellStoreRestoreData _restoreData = null!;
    private readonly IRegion _region;
    private readonly bool _clearHiddenCells;

    /// <summary>
    /// Command that merges the cells in the range give.
    /// Note that the value in the top LHS will be kept, while other cell values will be cleared.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="clearHiddenCells">Whether the cells the merge hides are cleared. When false they keep
    /// their values, as when only formats are pasted.</param>
    public MergeCellsCommand(IRegion region, bool clearHiddenCells = true)
    {
        _region = region.Clone();
        _clearHiddenCells = clearHiddenCells;
    }

    protected override bool CanExecuteCore(Sheet sheet)
    {
        var existingMerges = sheet.Cells.GetMerges(_region).ToList();
        if (!existingMerges.All(x => _region.Contains(x)))
            return false;
        return true;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        sheet.BatchUpdates();
        _overridenMergedRegions.Clear();
        _mergesPerformed.Clear();

        var region = _region;
        // Determine if there are any merged cells in the region
        // We can only merge over merged cells if we entirely overlap them
        var existingMerges = sheet.Cells.GetMerges(region).ToList();
        if (!existingMerges.All(x => region.Contains(x)))
            return false;

        // Clear all the cells that are not the top-left posn of merge and store their values for undo
        var regionsToClear = region
            .Break(region.TopLeft)
            .ToList();

        _restoreData = _clearHiddenCells ? sheet.Cells.ClearCellsImpl(regionsToClear) : new CellStoreRestoreData();

        // Store the merges that we will have to re-instate on undo
        // And remove any merges that are contained in the region
        _overridenMergedRegions.AddRange(existingMerges);
        foreach (var existingMerge in existingMerges)
        {
            sheet.Cells.UnMergeCellsImpl(new SheetRange(sheet, existingMerge));
        }

        // Store the merge that we are doing and perform the actual merge
        _mergesPerformed.Add(region);
        sheet.Cells.MergeImpl(region);
        sheet.EndBatchUpdates();
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.BatchUpdates();

        // Undo the merge we performed
        foreach (var merge in _mergesPerformed)
            sheet.Cells.UnMergeCellsImpl(merge);
        // Restore all the merges we removed
        foreach (var removedMerge in _overridenMergedRegions)
            sheet.Cells.MergeImpl(removedMerge);

        // Restore all the cell values that were lost when merging
        sheet.Cells.Restore(_restoreData);
        sheet.EndBatchUpdates();

        return true;
    }
}