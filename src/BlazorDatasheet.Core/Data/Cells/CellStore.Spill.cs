using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    /// <summary>
    /// The regions that array formulas have spilled into (or wanted to, when blocked). The formula is at
    /// the top-left of each region. Spilled regions never overlap, though a blocked region may overlap
    /// anything.
    /// </summary>
    /// <remarks>
    /// Spills are derived from formulas, like calculated values, so they are not undone directly: undo
    /// restores the formula or its absence, and the recalculation that follows re-spills or retracts.
    /// What the store guarantees is that a cell without a formula never owns a spill.
    /// </remarks>
    private readonly RegionDataStore<SpillInfo> _spillStore = new(0, expandWhenInsertAfter: false);

    /// <summary>
    /// Fired when the region an array formula spills into is set, changed or removed.
    /// </summary>
    public event EventHandler<SpillChangedEventArgs>? SpillChanged;

    internal RegionDataStore<SpillInfo> GetSpillStore() => _spillStore;

    private static CellPosition AnchorOf(DataRegion<SpillInfo> entry) => entry.Region.TopLeft;

    /// <summary>
    /// Returns the region spilled into that contains the position, including when the position is the
    /// formula cell itself, or null if the cell is not part of a spill.
    /// </summary>
    public IRegion? GetSpillRegion(int row, int col)
    {
        if (_spillStore.IsEmpty)
            return null;

        foreach (var entry in _spillStore.GetDataRegions(row, col))
        {
            if (!entry.Data.IsBlocked)
                return entry.Region;
        }

        return null;
    }

    /// <summary>
    /// Returns the position of the array formula whose spill contains the position, or null.
    /// </summary>
    public CellPosition? GetSpillAnchor(int row, int col)
    {
        var region = GetSpillRegion(row, col);
        return region?.TopLeft;
    }

    /// <summary>
    /// Returns the region the array formula at the position has spilled into, or null if the cell is not
    /// a spilling formula (including when its spill is blocked).
    /// </summary>
    public IRegion? GetSpillRegionForAnchor(int row, int col)
    {
        var entry = GetSpillEntry(new CellPosition(row, col));
        if (entry == null || entry.Data.IsBlocked)
            return null;
        return entry.Region;
    }

    /// <summary>
    /// Whether the cell holds an array formula that has spilled into other cells.
    /// </summary>
    public bool IsSpillAnchor(int row, int col) => GetSpillRegionForAnchor(row, col) != null;

    /// <summary>
    /// Whether the cell's value was written by an array formula in another cell. Such cells cannot be
    /// edited.
    /// </summary>
    public bool IsSpilledCell(int row, int col)
    {
        var anchor = GetSpillAnchor(row, col);
        return anchor != null && anchor.Value != new CellPosition(row, col);
    }

    /// <summary>
    /// Whether any array formula has spilled into the region.
    /// </summary>
    public bool AnySpills(IRegion region)
    {
        if (_spillStore.IsEmpty)
            return false;
        return _spillStore.GetDataRegions(region).Any(x => !x.Data.IsBlocked);
    }

    /// <summary>
    /// The entry (spilled or blocked) for the formula at <paramref name="anchor"/>, or null.
    /// </summary>
    internal DataRegion<SpillInfo>? GetSpillEntry(CellPosition anchor)
    {
        if (_spillStore.IsEmpty)
            return null;

        foreach (var entry in _spillStore.GetDataRegions(anchor.row, anchor.col))
        {
            if (AnchorOf(entry) == anchor)
                return entry;
        }

        return null;
    }

    /// <summary>
    /// The formulas whose spill (or intended spill, when blocked) overlaps the region. A change inside
    /// the region may unblock or obstruct them.
    /// </summary>
    internal IEnumerable<CellPosition> GetSpillAnchorsIntersecting(IRegion region)
    {
        if (_spillStore.IsEmpty)
            return [];
        return _spillStore.GetDataRegions(region).Select(AnchorOf).Distinct();
    }

    /// <summary>
    /// Records that the formula at <paramref name="anchor"/> spills into <paramref name="region"/>, or
    /// wanted to when <paramref name="blocked"/>. Replaces any entry it had before.
    /// </summary>
    internal void SetSpillImpl(CellPosition anchor, IRegion region, bool blocked)
    {
        var old = GetSpillEntry(anchor);
        var oldRegion = old == null || old.Data.IsBlocked ? null : old.Region;
        if (old != null)
            _spillStore.Delete(old);

        _spillStore.Add(region.Clone(), new SpillInfo(blocked));

        var newRegion = blocked ? null : region;
        if (oldRegion != null)
            Sheet.MarkDirty(oldRegion);
        if (newRegion != null)
            Sheet.MarkDirty(newRegion);
        SpillChanged?.Invoke(this, new SpillChangedEventArgs(anchor, oldRegion, newRegion));
    }

    /// <summary>
    /// Removes the spill entry of the formula at <paramref name="anchor"/>. Returns the region it had
    /// spilled into, or null if it had none or was blocked.
    /// </summary>
    internal IRegion? ClearSpillImpl(CellPosition anchor)
    {
        var entry = GetSpillEntry(anchor);
        if (entry == null)
            return null;

        _spillStore.Delete(entry);
        var oldRegion = entry.Data.IsBlocked ? null : entry.Region;
        if (oldRegion != null)
        {
            Sheet.MarkDirty(oldRegion);
            SpillChanged?.Invoke(this, new SpillChangedEventArgs(anchor, oldRegion, null));
        }

        return oldRegion;
    }

    /// <summary>
    /// Empties the cells the formula at <paramref name="anchor"/> spilled into (not the anchor itself)
    /// and removes its entry. Called when the formula goes away.
    /// </summary>
    internal void WipeSpillForAnchor(CellPosition anchor)
    {
        var entry = GetSpillEntry(anchor);
        if (entry == null)
            return;

        if (!entry.Data.IsBlocked)
            ClearSpilledValues(entry.Region, anchor);
        ClearSpillImpl(anchor);
    }

    /// <summary>
    /// Before anything other than the formula engine writes into <paramref name="region"/>: empties the
    /// cells of every spill overlapping it and marks those spills blocked, keeping their regions. The
    /// write then records the emptied cells as empty for undo, and the anchors recalculate afterwards.
    /// </summary>
    internal void WipeSpillsIntersecting(IRegion region)
    {
        if (_spillStore.IsEmpty)
            return;

        var entries = _spillStore.GetDataRegions(region).Where(x => !x.Data.IsBlocked).ToList();
        foreach (var entry in entries)
        {
            var anchor = AnchorOf(entry);
            ClearSpilledValues(entry.Region, anchor);
            SetSpillImpl(anchor, entry.Region, blocked: true);
        }
    }

    internal void WipeSpillsIntersecting(IEnumerable<IRegion> regions)
    {
        foreach (var region in regions)
            WipeSpillsIntersecting(region);
    }

    /// <summary>
    /// Empties every cell of <paramref name="region"/> except <paramref name="anchor"/>, and except
    /// cells inside <paramref name="keep"/> if given. Values are engine-owned, so no undo data is kept.
    /// </summary>
    internal void ClearSpilledValues(IRegion region, CellPosition anchor, IRegion? keep = null)
    {
        for (var row = region.Top; row <= region.Bottom; row++)
        {
            for (var col = region.Left; col <= region.Right; col++)
            {
                if (row == anchor.row && col == anchor.col)
                    continue;
                if (keep != null && keep.Contains(row, col))
                    continue;
                if (_dataStore.Get(row, col)!.IsEmpty && _validStore.Get(row, col) == null)
                    continue;
                SetCalculatedValueImpl(row, col, CellValue.Empty);
            }
        }
    }
}
