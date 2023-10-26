using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Restore;

internal class CellStoreRestoreData
{
    internal MatrixRestoreData<object?> ValueRestoreData { get; set; } = new();
    internal MatrixRestoreData<CellFormula?> FormulaRestoreData { get; set; } = new();
    internal MatrixRestoreData<bool> ValidRestoreData { get; set; } = new();
    public RegionRestoreData<string> TypeRestoreData { get; set; } = new();

    public IEnumerable<(int row, int col)> GetAffectedPositions()
    {
        return ValidRestoreData.DataRemoved.Select(x => (x.row, x.col))
            .Concat(ValueRestoreData.DataRemoved.Select(x => (x.row, x.col))
                .Concat(FormulaRestoreData.DataRemoved.Select(x => (x.row, x.col))));
    }

    public IEnumerable<IRegion> GetAffectedRegions()
    {
        return TypeRestoreData.RegionsAdded.Select(x => x.Region)
            .Concat(TypeRestoreData.RegionsRemoved.Select(x => x.Region));
    }
}