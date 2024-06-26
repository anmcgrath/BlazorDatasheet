using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    private readonly ConsolidatedDataStore<string> _typeStore = new();

    public string GetCellType(int row, int col)
    {
        var type = _typeStore.Get(row, col);
        return type ?? "default";
    }

    /// <summary>
    /// Sets the cell type in a region, to the value specified.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal CellStoreRestoreData SetCellTypeImpl(IRegion region, string? type)
    {
        var restoreData = new CellStoreRestoreData();
        if (string.IsNullOrEmpty(type))
            restoreData.TypeRestoreData = _typeStore.Clear(region);
        else
            restoreData.TypeRestoreData = _typeStore.Add(region, type);
        _sheet.MarkDirty(region);
        return restoreData;
    }

    /// <summary>
    /// Sets the cell type in a <paramref name="region"/>, to the <paramref name="type"/> specified.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public void SetType(IRegion region, string type)
    {
        _sheet.Commands.ExecuteCommand(new SetTypeCommand(region, type));
    }

    /// <summary>
    /// Sets the cell type in a <paramref name="region"/>, to the <paramref name="type"/> specified.
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public void SetType(IEnumerable<IRegion> regions, string type)
    {
        _sheet.Commands.BeginCommandGroup();
        foreach (var region in regions)
        {
            _sheet.Commands.ExecuteCommand(new SetTypeCommand(region, type));
        }

        _sheet.Commands.EndCommandGroup();
    }

    /// <summary>
    /// Sets the cell type for a <paramref name="row"/> amd <paramref name="col"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public void SetType(int row, int col, string type) => SetType(new Region(row, col), type);

    internal ConsolidatedDataStore<string> GetTypeStore() => _typeStore;
}