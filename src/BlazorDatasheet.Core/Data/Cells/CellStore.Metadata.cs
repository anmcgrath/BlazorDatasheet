using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Metadata;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    // Unlike formats, metadata belongs to its cell: cells inserted next to it don't take it on.
    private readonly MergeRegionDataStore<CellMetadata> _metaDataStore = new(expandOnInsert: false);

    /// <summary>
    /// Sets cell metadata, specified by name, for the cell at position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns>Whether setting the cell metadata was successful</returns>
    public bool SetCellMetaData(int row, int col, string name, object? value)
    {
        var cmd = new SetMetaDataCommand(row, col, name, value);
        return Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the same metadata value on every cell in a region as one undoable operation.
    /// Other metadata keys are preserved. A null value clears the named key.
    /// </summary>
    /// <param name="region">The region, which must be entirely within the sheet. Whole rows and columns are
    /// clipped to the sheet.</param>
    /// <param name="name">The metadata key to set.</param>
    /// <param name="value">The value shared by every cell, or null to clear the key.</param>
    /// <returns>Whether the command succeeded. An out-of-sheet region is rejected without changes.</returns>
    public bool SetCellMetaData(IRegion region, string name, object? value)
    {
        var cmd = new SetRegionMetaDataCommand(region, name, value);
        return Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Clears all metadata for the cell at position row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns>Whether clearing the cell metadata was successful</returns>
    public bool ClearCellMetaData(int row, int col)
    {
        var cmd = new ClearMetaDataCommand(row, col);
        return Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Clears the metadata specified by name for the cell at position row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="name"></param>
    /// <returns>Whether clearing the cell metadata was successful</returns>
    public bool ClearCellMetaData(int row, int col, string name)
    {
        return SetCellMetaData(row, col, name, null);
    }

    internal void SetMetaDataImpl(int row, int col, string name, object? value)
    {
        var oldValue = GetMetaData(row, col, name);
        var newMetaData = GetCellMetaData(row, col)?.Clone() ?? new CellMetadata();
        newMetaData.SetItem(name, value);
        SetMetaDataImpl(row, col, newMetaData);
        MetaDataChanged?.Invoke(this,
            new CellMetaDataChangeEventArgs(row, col, name, oldValue, value));
    }

    internal void ClearMetaDataImpl(int row, int col)
    {
        var oldMetaData = GetCellMetaData(row, col);
        if (oldMetaData == null)
            return;

        _metaDataStore.Clear(new Region(row, col));
        foreach (var item in oldMetaData.GetItems())
        {
            MetaDataChanged?.Invoke(this,
                new CellMetaDataChangeEventArgs(row, col, item.Key, item.Value, null));
        }

        Sheet.MarkDirty(new Region(row, col));
    }

    internal void ClearMetaDataImpl(int row, int col, string name)
    {
        SetMetaDataImpl(row, col, name, null);
    }

    internal void SetMetaDataImpl(int row, int col, CellMetadata? metaData)
    {
        _metaDataStore.Clear(new Region(row, col));
        if (metaData?.IsEmpty == false)
            _metaDataStore.Add(new Region(row, col), metaData);
        Sheet.MarkDirty(new Region(row, col));
    }

    /// <summary>
    /// Returns the metadata with key "name" for the cell at row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public object? GetMetaData(int row, int col, string name)
    {
        return GetCellMetaData(row, col)?.GetItem(name);
    }

    internal CellMetadata? GetCellMetaData(int row, int col) => _metaDataStore.GetFirstData(row, col);

    internal MergeRegionDataStore<CellMetadata> GetMetaDataStore() => _metaDataStore;

    /// <summary>
    /// Whether anything listens to <see cref="MetaDataChanged"/>, so that snapshots taken only to fire it can be skipped.
    /// </summary>
    internal bool HasMetaDataListeners => MetaDataChanged != null;

    /// <summary>
    /// The metadata items of the cells in <paramref name="regions"/>, keyed by cell and name. Only the cells
    /// that have metadata are visited.
    /// </summary>
    internal Dictionary<(int row, int col, string name), object> GetMetaDataItems(IEnumerable<IRegion> regions)
    {
        var items = new Dictionary<(int row, int col, string name), object>();
        foreach (var region in regions)
        foreach (var dataRegion in _metaDataStore.GetDataRegions(region))
        {
            if (dataRegion.Region.GetIntersection(region) is not { } overlap)
                continue;
            for (var row = overlap.Top; row <= overlap.Bottom; row++)
            for (var col = overlap.Left; col <= overlap.Right; col++)
            {
                if (GetCellMetaData(row, col) is not { } metadata)
                    continue;
                foreach (var item in metadata.GetItems())
                    items[(row, col, item.Key)] = item.Value;
            }
        }

        return items;
    }

    /// <summary>
    /// Fires <see cref="MetaDataChanged"/> for every item that differs between two snapshots taken with
    /// <see cref="GetMetaDataItems"/>.
    /// </summary>
    internal void EmitMetaDataChanges(Dictionary<(int row, int col, string name), object> before,
        Dictionary<(int row, int col, string name), object> after)
    {
        foreach (var key in before.Keys.Union(after.Keys))
        {
            before.TryGetValue(key, out var oldValue);
            after.TryGetValue(key, out var newValue);
            if (!Equals(oldValue, newValue))
                MetaDataChanged?.Invoke(this,
                    new CellMetaDataChangeEventArgs(key.row, key.col, key.name, oldValue, newValue));
        }
    }
}
