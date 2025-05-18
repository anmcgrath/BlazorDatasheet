using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

public class UnMergeCellsCommand : BaseCommand, IUndoableCommand
{
    private readonly List<IRegion> _unMergesPerformed = new();
    private readonly IRegion _region;

    /// <summary>
    /// Command that unmerges the cells in the range given.
    /// Note that the value in the merged range will return to the top left cell, while the
    ///   other cells will be blank.
    /// </summary>
    /// <param name="region">The region in which to merge. </param>
    public UnMergeCellsCommand(IRegion region)
    {
        _region = region.Clone();
    }

    public override bool CanExecute(Sheet sheet)
    {
        var existingMerges = sheet.Cells.GetMerges(_region).ToList();
        if (existingMerges.All(x => _region.Contains(x)))
            return true;
        return false;
    }

    public override bool Execute(Sheet sheet)
    {
        sheet.BatchUpdates();
        _unMergesPerformed.Clear();

        var region = _region;

        // Clear all the cells that are not the top-left posn of merge and store their values for undo
        var regionsToClear = region
            .Break(region.TopLeft)
            .ToList();

        // Store the unmerge that we are doing and perform the actual unmerge
        _unMergesPerformed.Add(region);
        sheet.Cells.UnMergeCellsImpl(region);

        sheet.EndBatchUpdates();
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.BatchUpdates();

        // Redo the merge we performed before the unmerge
        foreach (var merge in _unMergesPerformed)
            sheet.Cells.MergeImpl(merge);

        sheet.EndBatchUpdates();

        return true;
    }
}