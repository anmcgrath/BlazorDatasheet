using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
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
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    internal void SetMetaDataImpl(int row, int col, string name, object? value)
    {
        var newMetaData = new CellMetadata();
        newMetaData.SetItem(name, value);
        _metaDataStore.Add(new Region(row, col), newMetaData);
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
        var container = _metaDataStore.GetData(row, col).FirstOrDefault() ?? new CellMetadata();
        return container.GetItem(name);
    }
}