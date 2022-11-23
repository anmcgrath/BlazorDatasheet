using BlazorDatasheet.Data;
using BlazorDatasheet.Data.SpatialDataStructures;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Commands;

public class MergeCellsCommand : IUndoableCommand
{
    private readonly BRange _range;
    private List<IRegion> _overridenMergedRegions = new();
    private List<IRegion> _mergesPerformed = new();
    private List<ValueChange> _changes = new();

    public MergeCellsCommand(BRange range)
    {
        _range = range;
    }


    public bool Execute(Sheet sheet)
    {
        _overridenMergedRegions = new List<IRegion>();
        _changes = new List<ValueChange>();

        foreach (var region in _range.Regions)
        {
            var envelope = region.ToEnvelope();
            // Determine if there are any merged cells in the region
            // We can only merge over merged cells if we entirely overlap them
            var existingMerges = sheet.MergedCells.Search(envelope);
            if (!existingMerges.All(x => region.Contains(x.Region)))
                continue;

            // Clear all the cells that are not the top-left posn of merge and store their values for undo
            var cellsToClear = region
                               .Break(region.TopLeft)
                               .SelectMany(sheet.GetNonEmptyCellPositions)
                               .ToList();

            _changes.AddRange(cellsToClear.Select(x => getValueChangeOnClear(x.row, x.col, sheet)));
            sheet.ClearCellsImpl(cellsToClear);

            // Store the merges that we will have to re-instate on undo
            // And remove any merges that are contained in the region
            _overridenMergedRegions.AddRange(existingMerges.Select(x => x.Region));
            sheet.UnMergeCellsImpl(new BRange(sheet, existingMerges.Select(x => x.Region)));

            // Store the merge that we are doing and perform the actual merge
            _mergesPerformed.Add(region);
            sheet.MergeCellsImpl(region);
        }

        return true;
    }

    private ValueChange getValueChangeOnClear(int row, int col, Sheet sheet)
    {
        return new ValueChange(row, col, sheet.GetValue(row, col));
    }

    public bool Undo(Sheet sheet)
    {
        // Undo the merge we performed
        foreach (var merge in _mergesPerformed)
            sheet.UnMergeCellsImpl(merge);
        // Restore all the merges we removed
        foreach (var removedMerge in _overridenMergedRegions)
            sheet.MergeCellsImpl(removedMerge);

        // Restore all the cell values that were lost when merging
        sheet.SetCellValuesImpl(_changes);

        return true;
    }
}