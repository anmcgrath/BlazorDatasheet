using BlazorDatasheet.Commands;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Events;

namespace BlazorDatasheet.Data;

public class MergeManager
{
    private readonly Sheet _sheet;

    /// <summary>
    /// Fired when cells are merged
    /// </summary>
    public event EventHandler<IRegion>? RegionMerged;

    /// <summary>
    /// Fired when cells are un-merged
    /// </summary>
    public event EventHandler<IRegion>? RegionUnMerged;

    /// <summary>
    /// The merged cells in the sheet.
    /// </summary>
    internal RegionDataStore<bool> Store { get; } = new RegionDataStore<bool>(1, false);

    public MergeManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    internal bool AddImpl(BRange range)
    {
        var isSuccess = true;
        foreach (var region in range.Regions)
        {
            isSuccess &= AddImpl(region);
        }

        return isSuccess;
    }

    internal bool AddImpl(IRegion region)
    {
        Store.Add(region, true);
        RegionMerged?.Invoke(this, region);
        return true;
    }

    /// <summary>
    /// Add a range as a merged cell. If the range overlaps any existing merged cells, the merge
    /// will not happen.
    /// </summary>
    /// <param name="range"></param>
    public void Add(BRange range)
    {
        var merge = new MergeCellsCommand(range);
        _sheet.Commands.ExecuteCommand(merge);
    }

    /// <summary>
    /// Add a region as a merged cell. If the range overlaps any existing merged cells, the merge
    /// will not happen.
    /// </summary>
    /// <param name="region"></param>
    public void Add(IRegion region) => Add(new BRange(_sheet, region));

    /// <summary>
    /// Un-merge all cells that overlap the range
    /// </summary>
    /// <param name="region"></param>
    internal void UnMergeCellsImpl(IRegion region)
    {
        var mergedCellsInRange = Store.GetRegionsOverlapping(region);
        foreach (var merge in mergedCellsInRange)
        {
            Store.Delete(merge);
            RegionUnMerged?.Invoke(this, region);
        }
    }

    /// <summary>
    /// Un-merge all cells that overlap the range
    /// </summary>
    /// <param name="region"></param>
    internal void UnMergeCellsImpl(BRange range)
    {
        foreach (var region in range.Regions)
            UnMergeCellsImpl((IRegion)region);
    }

    /// <summary>
    /// Returns whether the position is inside a merged cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsInsideMerge(int row, int col)
    {
        return Get(row, col) != null;
    }

    /// <summary>
    /// Returns the region (if any) that exists at the given position.
    /// There will only be one region at most, because merges cannot overlap.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IRegion? Get(int row, int col)
    {
        var merges = Store.GetRegionsOverlapping(row, col).ToList();
        // There will only be one merge because we don't allow overlapping
        return merges.Any() ? merges[0].Region : null;
    }

    /// <summary>
    /// Returns whether the sheet has any merged cells.
    /// </summary>
    /// <returns></returns>
    public bool Any()
    {
        return Store.Any();
    }
}