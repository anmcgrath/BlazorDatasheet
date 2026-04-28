using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Metadata;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    private readonly MergeRegionDataStore<CellMetadata> _metaDataStore = new();

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
        var container = GetCellMetaData(row, col) ?? new CellMetadata();
        return container.GetItem(name);
    }

    internal CellMetadata? GetCellMetaData(int row, int col) => _metaDataStore.GetData(row, col).FirstOrDefault();

    internal MergeRegionDataStore<CellMetadata> GetMetaDataStore() => _metaDataStore;
}
