using System.Runtime.CompilerServices;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    public Sheet Sheet { get; }

    private readonly CellValue _defaultCellValue = new(null);

    public CellStore(Sheet sheet)
    {
        Sheet = sheet;
        _dataStore = new SparseMatrixStoreByRows<CellValue>(_defaultCellValue);
    }

    /// <summary>
    /// Returns all cells in the specified region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegion(IRegion region)
    {
        return (new SheetRange(Sheet, region))
            .Positions
            .Select(x => this.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Returns all cells that are present in the regions given.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegions(IEnumerable<IRegion> regions)
    {
        var cells = new List<IReadOnlyCell>();
        foreach (var region in regions)
            cells.AddRange(GetCellsInRegion(region));
        return cells;
    }

    /// <summary>
    /// Returns the cell at the specified position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(int row, int col)
    {
        return new SheetCell(row, col, Sheet);
    }

    /// <summary>
    /// Returns the cell at the specified position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(CellPosition position)
    {
        return GetCell(position.row, position.col);
    }

    internal IEnumerable<CellPosition> GetNonEmptyCellPositions(IRegion region)
    {
        return _dataStore.GetNonEmptyPositions(region.TopLeft.row,
            region.BottomRight.row,
            region.TopLeft.col,
            region.BottomRight.col);
    }

    internal IEnumerable<(int row, int col, CellValue value)> GetNonEmptyCellValues(IRegion region)
    {
        return _dataStore.GetNonEmptyData(region);
    }

    /// <summary>
    /// Clears all cell values in the region
    /// </summary>
    /// <param name="regions"></param>
    public void ClearCells(IEnumerable<IRegion> regions)
    {
        var cmd = new ClearCellsCommand(regions);
        Sheet.Commands.ExecuteCommand(cmd);
    }

    public void ClearCells(IRegion region) => ClearCells(new[] { region });

    internal CellStoreRestoreData ClearCellsImpl(IEnumerable<IRegion> regionsToClear)
    {
        var restoreData = new CellStoreRestoreData();
        var toClear = regionsToClear.ToList();
        restoreData.ValueRestoreData = _dataStore.Clear(toClear);
        restoreData.ValidRestoreData = _validStore.Clear(toClear);
        restoreData.Merge(ClearFormulaImpl(toClear));

        var affected = restoreData.GetAffectedPositions().ToList();
        Sheet.BatchUpdates();
        EmitCellsChanged(toClear);
        Sheet.MarkDirty(toClear);
        Sheet.EndBatchUpdates();
        return restoreData;
    }

    internal CellStoreRestoreData InsertRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new CellStoreRestoreData
        {
            ValueRestoreData = _dataStore.InsertRowColAt(index, count, axis),
            FormatRestoreData = _formatStore.InsertRowColAt(index, count, axis),
            TypeRestoreData = _typeStore.InsertRowColAt(index, count, axis),
            ValidRestoreData = _validStore.InsertRowColAt(index, count, axis),
            MergeRestoreData = _mergeStore.InsertRowColAt(index, count, axis),
            FormulaRestoreData = _formulaStore.InsertRowColAt(index, count, axis),
            DependencyManagerRestoreData =
                Sheet.FormulaEngine.DependencyManager.InsertRowColAt(index, count, axis, Sheet.Name)
        };


        return restoreData;
    }

    internal CellStoreRestoreData RemoveRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new CellStoreRestoreData
        {
            ValueRestoreData = _dataStore.RemoveRowColAt(index, count, axis),
            ValidRestoreData = _validStore.RemoveRowColAt(index, count, axis),
            TypeRestoreData = _typeStore.RemoveRowColAt(index, count, axis),
            FormatRestoreData = _formatStore.RemoveRowColAt(index, count, axis),
            MergeRestoreData = _mergeStore.RemoveRowColAt(index, count, axis),
            FormulaRestoreData = _formulaStore.RemoveRowColAt(index, count, axis),
            DependencyManagerRestoreData =
                Sheet.FormulaEngine.DependencyManager.RemoveRowColAt(index, count, axis, Sheet.Name)
        };

        return restoreData;
    }

    internal CellStoreRestoreData CopyImpl(IRegion fromRegion, IRegion toRegion, CopyOptions options)
    {
        var restoreData = new CellStoreRestoreData();
        if (options.CopyValues)
        {
            restoreData.ValueRestoreData = _dataStore.Copy(fromRegion, toRegion);
            restoreData.ValidRestoreData = _validStore.Copy(fromRegion, toRegion);
            restoreData.FormulaRestoreData = _formulaStore.Clear(toRegion);
        }

        if (options.CopyFormula)
            restoreData.Merge(CopyFormulaImpl(fromRegion, toRegion));

        if (options.CopyFormat)
            restoreData.Merge(CopyFormatImpl(fromRegion, toRegion));

        Sheet.MarkDirty(toRegion);
        EmitCellsChanged(toRegion);
        return restoreData;
    }

    internal CellStoreRestoreData CopyFormatImpl(IRegion fromRegion, IRegion toRegion)
    {
        // Here we create a blank format store and then fill it with the 
        // values that we sample from the sheet.
        // We then perform the copy inside this new format store and remove the original data.
        // This is then added to the sheet's format store.
        var emptyPalette = new MergeRegionDataStore<CellFormat>();
        var fixedFromRegion = fromRegion.GetIntersection(Sheet.Region) as Region;
        foreach (var position in fixedFromRegion!)
        {
            emptyPalette.Add(new Region(position.row, position.col), Sheet.GetFormat(position.row, position.col));
        }

        emptyPalette.Copy(fromRegion, toRegion.TopLeft);
        var toClear = fromRegion.Break(toRegion);
        foreach (var region in toClear)
            emptyPalette.Clear(region);


        var restoreData = CutFormatImpl(toRegion);
        foreach (var data in emptyPalette.GetAllDataRegions())
        {
            restoreData.Merge(MergeFormatImpl(data.Region, data.Data));
        }

        return restoreData;
    }

    /// <summary>
    /// Returns true if any cell format in the region is readonly.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public bool ContainsReadOnly(IRegion region)
    {
        foreach (var interval in Sheet.Rows.Formats.GetIntervals(region.Top, region.Bottom))
            if (interval.Data.IsReadOnly == true)
                return true;

        foreach (var interval in Sheet.Columns.Formats.GetIntervals(region.Left, region.Right))
            if (interval.Data.IsReadOnly == true)
                return true;

        foreach (var cellFormat in _formatStore.GetData(region))
            if (cellFormat.IsReadOnly == true)
                return true;

        return false;
    }

    /// <summary>
    /// Restores the internal storage state by redoing any actions that caused the internal data to change.
    /// Fires events for the changed data.
    /// </summary>
    /// <param name="restoreData"></param>
    internal void Restore(CellStoreRestoreData restoreData)
    {
        Sheet.BatchUpdates();

        // Set formula through this function so we add the formula back in to the dependency graph
        Sheet.FormulaEngine.DependencyManager.Restore(restoreData.DependencyManagerRestoreData);

        _formulaStore.Restore(restoreData.FormulaRestoreData);
        _validStore.Restore(restoreData.ValidRestoreData);
        _typeStore.Restore(restoreData.TypeRestoreData);
        _dataStore.Restore(restoreData.ValueRestoreData);
        _formatStore.Restore(restoreData.FormatRestoreData);
        _mergeStore.Restore(restoreData.MergeRestoreData);

        foreach (var pt in restoreData.ValueRestoreData.DataRemoved)
        {
            Sheet.MarkDirty(pt.row, pt.col);
            EmitCellChanged(pt.row, pt.col);
        }

        foreach (var region in restoreData.GetAffectedRegions())
        {
            Sheet.MarkDirty(region);
        }

        Sheet.EndBatchUpdates();
    }


    /// <summary>
    /// The <see cref="SheetCell"/> at position row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public SheetCell this[int row, int col]
    {
        get { return new SheetCell(row, col, Sheet); }
    }

    /// <summary>
    /// The <see cref="SheetCell"/> at the address given. If the address is not valid for a cell, null is returned.
    /// </summary>
    /// <param name="cellAddress"></param>
    public SheetCell? this[string cellAddress]
    {
        get
        {
            if (string.IsNullOrEmpty(cellAddress))
                return null;

            var rangeStrFormula = $"={cellAddress}";
            var evaluatedValue =
                Sheet.FormulaEngine.Evaluate(Sheet.FormulaEngine.ParseFormula(rangeStrFormula, Sheet.Name),
                    resolveReferences: false);
            if (evaluatedValue.ValueType == CellValueType.Reference)
            {
                var reference = evaluatedValue.GetValue<Reference>();
                if (reference?.Kind == ReferenceKind.Cell)
                    return new SheetCell(reference.Region!.Top, reference.Region!.Left, Sheet);
            }

            return null;
        }
    }
}