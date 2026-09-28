using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    public event EventHandler<FormatChangedEventArgs>? FormatChanged;

    /// <summary>
    /// Stores individual cell formats.
    /// </summary>
    private readonly MergeRegionDataStore<CellFormat> _formatStore = new();

    /// <summary>
    /// Merges the new cell format into any existing formats
    /// </summary>
    /// <param name="region"></param>
    /// <param name="format"></param>
    internal CellStoreRestoreData MergeFormatImpl(IRegion region, CellFormat format)
    {
        var restoreData = new CellStoreRestoreData()
        {
            FormatRestoreData = _formatStore.Add(region, format)
        };
        // outside a batch this renders the cells straight away, so the format has to be stored first.
        Sheet.MarkDirty(region);
        FormatChanged?.Invoke(this, new FormatChangedEventArgs(region, format));
        return restoreData;
    }

    /// <summary>
    /// Merges the format into each of the regions, as one change to the region that spans them:
    /// the cells are marked dirty and <see cref="FormatChanged"/> is raised once rather than per region.
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="format"></param>
    /// <param name="spanningRegion">The region that contains all of <paramref name="regions"/></param>
    /// <returns>The restore data of each merge, in the order they were made.</returns>
    internal List<CellStoreRestoreData> MergeFormatImpl(IReadOnlyList<IRegion> regions, CellFormat format,
        IRegion spanningRegion)
    {
        var restoreData = new List<CellStoreRestoreData>(regions.Count);
        if (regions.Count == 0)
            return restoreData;

        foreach (var region in regions)
            restoreData.Add(new CellStoreRestoreData() { FormatRestoreData = _formatStore.Add(region, format) });

        Sheet.MarkDirty(spanningRegion);
        FormatChanged?.Invoke(this, new FormatChangedEventArgs(spanningRegion, format));
        return restoreData;
    }

    internal CellStoreRestoreData CutFormatImpl(IRegion region)
    {
        var locks = Sheet.Protection.IsEnforced
            ? _formatStore.GetDataRegions(region)
                .Where(x => x.Data.IsLocked != null)
                .Select(x => (Region: x.Region.GetIntersection(region)!, Locked: x.Data.IsLocked)).ToList()
            : new();
        var restore = new CellStoreRestoreData { FormatRestoreData = _formatStore.Clear(region) };
        foreach (var item in locks)
            restore.Merge(MergeFormatImpl(item.Region, new CellFormat { IsLocked = item.Locked }));
        return restore;
    }

    /// <summary>
    /// Returns the CELL format that is assigned to the cell.
    /// Note this is not the visual format, because that will be merged with row/column formats.
    /// If the format is not assigned, the default (empty) format is returned.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public CellFormat? GetFormat(int row, int col)
    {
        return _formatStore.GetData(row, col).FirstOrDefault();
    }


    public IEnumerable<DataRegion<CellFormat>> GetFormatData(IRegion region)
    {
        return _formatStore.GetDataRegions(region);
    }

    internal MergeRegionDataStore<CellFormat> GetFormatStore() => _formatStore;
}