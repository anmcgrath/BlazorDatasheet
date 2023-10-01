using BlazorDatasheet.Commands;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Util;

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

    public MergeManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// The merged cells in the sheet.
    /// </summary>
    internal RTree<CellMerge> MergedCells { get; } = new();

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
        var cellMerge = new CellMerge(region);
        MergedCells.Insert(cellMerge);
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
        var envelope = region.ToEnvelope();

        var mergedCellsInRange = MergedCells.Search(envelope);
        foreach (var merge in mergedCellsInRange)
        {
            MergedCells.Delete(merge);
            RegionUnMerged?.Invoke(this, merge.Region);
        }
    }

    /// <summary>
    /// Un-merge all cells that overlap the range
    /// </summary>
    /// <param name="region"></param>
    internal void UnMergeCellsImpl(BRange range)
    {
        foreach (var region in range.Regions)
            UnMergeCellsImpl((BRange)region);
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
        var cellRegion = new Region(row, col);
        var merges = MergedCells.Search(cellRegion.ToEnvelope());
        // There will only be one merge because we don't allow overlapping
        return merges.Any() ? merges[0].Region : null;
    }

    /// <summary>
    /// Returns whether the sheet has any merged cells.
    /// </summary>
    /// <returns></returns>
    public bool Any()
    {
        return MergedCells.Count > 0;
    }

    /// <summary>
    /// Updates a merged regions after insert or remove rows or columns
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index">index of inserted row\column</param>
    /// <param name="count">count of inserted or removed rows\columns. count > 0 when inserted, count < 0 when reomved</param>
    /// <returns>list of affected regions (before operation state) and list of new regions (state after operation)</returns>
    internal (IReadOnlyList<CellMerge> mergesPerformed, IReadOnlyList<CellMerge> overridenMergedRegions)
        RerangeMergedCells(Axis axis, int index, int count)
    {
        var afterInserted = axis == Axis.Row
            ? new Region(index, _sheet.NumRows, 0, _sheet.NumCols)
            : new Region(0, _sheet.NumRows, index, _sheet.NumCols);
        var envelope = afterInserted.ToEnvelope();
        var mergesPerformed = MergedCells.Search(envelope);
        var overridenMergedRegions = new List<CellMerge>();
        foreach (var item in mergesPerformed)
        {
            // Ignore row or column regions because
            // they do not have a fixed end position
            if ((item.Region is RowRegion && axis == Axis.Col) || (item.Region is ColumnRegion && axis == Axis.Row))
                continue;

            var region = item.Region.Clone();

            if (axis == Axis.Row)
            {
                if (index < region.Top)
                {
                    region.Shift(count, 0);
                }
                else
                {
                    region.Expand(Edge.Bottom, count);
                }
            }
            else if (axis == Axis.Col)
            {
                if (index < region.Left)
                {
                    region.Shift(0, count);
                }
                else
                {
                    region.Expand(Edge.Right, count);
                }
            }

            MergedCells.Delete(item);

            if ((region.Top != region.Bottom && region.Left != region.Right) || region is RowRegion ||
                region is ColumnRegion)
            {
                var merge = new CellMerge(region);
                MergedCells.Insert(merge);
                overridenMergedRegions.Add(merge);
            }
        }

        return (mergesPerformed, overridenMergedRegions.AsReadOnly());
    }

    /// <summary>
    /// Undo rerange operation to restore state before Insert\Remove rows\columns commands
    /// </summary>
    /// <param name="_mergesPerformed">state to return on</param>
    /// <param name="_overridenMergedRegions">state to undo</param>
    internal void UndoRerangeMergedCells(IReadOnlyList<CellMerge> _mergesPerformed,
        IReadOnlyList<CellMerge> _overridenMergedRegions)
    {
        foreach (var item in _overridenMergedRegions)
        {
            MergedCells.Delete(item);
        }

        foreach (var item in _mergesPerformed)
        {
            MergedCells.Insert(item);
        }
    }
}