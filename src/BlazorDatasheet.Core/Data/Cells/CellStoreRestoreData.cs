using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Cells;

internal class CellStoreRestoreData
{
    internal MatrixRestoreData<object?> ValueRestoreData { get; set; } = new();
    internal MatrixRestoreData<CellFormula?> FormulaRestoreData { get; set; } = new();
    internal MatrixRestoreData<bool?> ValidRestoreData { get; set; } = new();
    internal RegionRestoreData<string> TypeRestoreData { get; set; } = new();
    internal RegionRestoreData<CellFormat> FormatRestoreData { get; set; } = new();
    internal RegionRestoreData<bool> MergeRestoreData { get; set; } = new();

    public IEnumerable<CellPosition> GetAffectedPositions()
    {
        return ValidRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))
            .Concat(ValueRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))
                .Concat(FormulaRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))));
    }

    public IEnumerable<IRegion> GetAffectedRegions()
    {
        return TypeRestoreData.RegionsAdded.Select(x => x.Region)
            .Concat(TypeRestoreData.RegionsRemoved.Select(x => x.Region))
            .Concat(FormatRestoreData.RegionsAdded.Select(x => x.Region))
            .Concat(FormatRestoreData.RegionsRemoved.Select(x => x.Region));
    }
}