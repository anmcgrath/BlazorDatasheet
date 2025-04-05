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
        Sheet.MarkDirty(region);
        var restoreData = new CellStoreRestoreData()
        {
            FormatRestoreData = _formatStore.Add(region, format)
        };
        FormatChanged?.Invoke(this, new FormatChangedEventArgs(region, format));
        return restoreData;
    }

    internal CellStoreRestoreData CutFormatImpl(IRegion region)
    {
        return new CellStoreRestoreData()
        {
            FormatRestoreData = _formatStore.Clear(region)
        };
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