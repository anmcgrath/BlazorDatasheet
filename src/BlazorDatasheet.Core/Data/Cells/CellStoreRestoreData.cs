using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.Data.Cells;

internal class CellStoreRestoreData
{
    internal MatrixRestoreData<CellValue> ValueRestoreData { get; set; } = new();
    internal MatrixRestoreData<CellFormula?> FormulaRestoreData { get; set; } = new();
    internal MatrixRestoreData<bool?> ValidRestoreData { get; set; } = new();
    internal RegionRestoreData<string> TypeRestoreData { get; set; } = new();
    internal RegionRestoreData<CellFormat> FormatRestoreData { get; set; } = new();
    internal RegionRestoreData<bool> MergeRestoreData { get; set; } = new();
    internal DependencyManagerRestoreData DependencyManagerRestoreData { get; set; } = new();

    internal IEnumerable<CellPosition> GetAffectedPositions()
    {
        return ValidRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))
            .Concat(ValueRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))
                .Concat(FormulaRestoreData.DataRemoved.Select(x => new CellPosition(x.row, x.col))));
    }

    internal IEnumerable<IRegion> GetAffectedRegions()
    {
        return TypeRestoreData.RegionsAdded.Select(x => x.Region)
            .Concat(TypeRestoreData.RegionsRemoved.Select(x => x.Region))
            .Concat(FormatRestoreData.RegionsAdded.Select(x => x.Region))
            .Concat(FormatRestoreData.RegionsRemoved.Select(x => x.Region));
    }

    internal void Merge(CellStoreRestoreData item)
    {
        ValueRestoreData.Merge(item.ValueRestoreData);
        FormulaRestoreData.Merge(item.FormulaRestoreData);
        ValidRestoreData.Merge(item.ValidRestoreData);
        TypeRestoreData.Merge(item.TypeRestoreData);
        FormatRestoreData.Merge(item.FormatRestoreData);
        MergeRestoreData.Merge(item.MergeRestoreData);
        DependencyManagerRestoreData.Merge(item.DependencyManagerRestoreData);
    }
}