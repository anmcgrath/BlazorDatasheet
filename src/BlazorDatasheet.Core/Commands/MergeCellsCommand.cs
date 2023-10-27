using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

public class MergeCellsCommand : IUndoableCommand
{
    private readonly BRange _range;
    private readonly List<IRegion> _overridenMergedRegions = new();
    private readonly List<IRegion> _mergesPerformed = new();
    private CellStoreRestoreData _restoreData;

    /// <summary>
    /// Command that merges the cells in the range give.
    /// Note that the value in the top LHS will be kept, while other cell values will be cleared.
    /// </summary>
    /// <param name="range">The range in which to merge. </param>
    public MergeCellsCommand(BRange range)
    {
        _range = range.Clone();
    }


    public bool Execute(Sheet sheet)
    {
        _overridenMergedRegions.Clear();
        _mergesPerformed.Clear();

        foreach (var region in _range.Regions)
        {
            // Determine if there are any merged cells in the region
            // We can only merge over merged cells if we entirely overlap them
            var existingMerges = sheet.Cells.GetMerges(region).ToList();
            if (!existingMerges.All(x => region.Contains(x)))
                continue;

            // Clear all the cells that are not the top-left posn of merge and store their values for undo
            var regionsToClear = region
                .Break(region.TopLeft)
                .ToList();

            _restoreData = sheet.Cells.ClearCellsImpl(regionsToClear);

            // Store the merges that we will have to re-instate on undo
            // And remove any merges that are contained in the region
            _overridenMergedRegions.AddRange(existingMerges);
            sheet.Cells.UnMergeCellsImpl(new BRange(sheet, existingMerges));

            // Store the merge that we are doing and perform the actual merge
            _mergesPerformed.Add(region);
            sheet.Cells.MergeImpl(region);
        }

        return true;
    }

    private CellValueChange getValueChangeOnClear(int row, int col, Sheet sheet)
    {
        return new CellValueChange(row, col, sheet.Cells.GetValue(row, col));
    }

    public bool Undo(Sheet sheet)
    {
        // Undo the merge we performed
        foreach (var merge in _mergesPerformed)
            sheet.Cells.UnMergeCellsImpl(merge);
        // Restore all the merges we removed
        foreach (var removedMerge in _overridenMergedRegions)
            sheet.Cells.MergeImpl(removedMerge);

        sheet.Selection.Set(_range);
        // Restore all the cell values that were lost when merging
        sheet.Cells.Restore(_restoreData);

        return true;
    }
}