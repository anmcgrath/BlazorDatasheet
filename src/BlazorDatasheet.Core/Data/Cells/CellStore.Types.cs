using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    private readonly ConsolidatedDataStore<string> _typeStore = new();

    public string GetCellType(int row, int col)
    {
        var type = _typeStore.Get(row, col);
        return type ?? "text";
    }

    /// <summary>
    /// Sets the cell type in a region, to the value specified.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal CellStoreRestoreData SetCellTypeImpl(IRegion region, string type)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.TypeRestoreData = _typeStore.Add(region, type);
        _sheet.MarkDirty(region);
        return restoreData;
    }

    /// <summary>
    /// Sets the cell type in a region, to the value specified.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public void SetCellType(IRegion region, string type)
    {
        _sheet.Commands.ExecuteCommand(new SetTypeCommand(region, type));
    }
}