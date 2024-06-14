using System.Diagnostics;
using System.Runtime.CompilerServices;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    private Sheet _sheet;
    private readonly CellValue _defaultCellValue = new(null);

    public CellStore(Sheet sheet)
    {
        _sheet = sheet;
        _dataStore = new SparseMatrixStoreByRows<CellValue>(_defaultCellValue);
    }

    /// <summary>
    /// Returns all cells in the specified region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegion(IRegion region)
    {
        return (new SheetRange(_sheet, region))
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
        return cells.ToArray();
    }

    /// <summary>
    /// Returns the cell at the specified position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(int row, int col)
    {
        return new SheetCell(row, col, _sheet);
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
        _sheet.Commands.ExecuteCommand(cmd);
    }

    public void ClearCells(IRegion region) => ClearCells(new[] { region });

    internal CellStoreRestoreData ClearCellsImpl(IEnumerable<IRegion> regionsToClear)
    {
        var restoreData = new CellStoreRestoreData();
        var toClear = regionsToClear.ToList();
        restoreData.ValueRestoreData = _dataStore.Clear(toClear);
        restoreData.ValidRestoreData = _validStore.Clear(toClear);
        restoreData.FormulaRestoreData = ClearFormulaImpl(toClear).FormulaRestoreData;

        var affected = restoreData.GetAffectedPositions().ToList();
        _sheet.BatchUpdates();
        EmitCellsChanged(affected);
        _sheet.MarkDirty(affected);
        _sheet.EndBatchUpdates();
        return restoreData;
    }

    /// <summary>
    /// Inserts a number of columns into each of the cell's stores.
    /// </summary>
    /// <param name="col">The column that will be replaced by the new column.</param>
    /// <param name="nCols">The number of columns to insert</param>
    /// <param name="expandNeighboring">Whether to expand any cell data to the left of the insertion. If undoing an action, best to set to false.</param>
    internal CellStoreRestoreData InsertColAt(int col, int nCols, bool? expandNeighboring = null)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.InsertColAt(col, nCols);
        restoreData.FormatRestoreData = _formatStore.InsertCols(col, nCols);
        restoreData.TypeRestoreData = _typeStore.InsertCols(col, nCols);
        restoreData.FormulaRestoreData = _formulaStore.InsertColAt(col, nCols);
        restoreData.ValidRestoreData = _validStore.InsertColAt(col, nCols);
        restoreData.MergeRestoreData = _mergeStore.InsertCols(col, nCols, false);
        return restoreData;
    }

    /// <summary>
    /// Inserts a number of rows into each of the cell's stores.
    /// </summary>
    /// <param name="row">The row that will be replaced by the new row.</param>
    /// <param name="nRows">The number of rows to insert</param>
    /// <param name="expandNeighboring">Whether to expand any cell data to the left of the insertion. If undoing an action, best to set to false.</param>
    internal CellStoreRestoreData InsertRowAt(int row, int nRows, bool? expandNeighboring = null)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.InsertRowAt(row, nRows);
        restoreData.FormatRestoreData = _formatStore.InsertRows(row, nRows, expandNeighboring);
        restoreData.TypeRestoreData = _typeStore.InsertRows(row, nRows, expandNeighboring);
        restoreData.FormulaRestoreData = _formulaStore.InsertRowAt(row, nRows);
        restoreData.ValidRestoreData = _validStore.InsertRowAt(row, nRows);
        restoreData.MergeRestoreData = _mergeStore.InsertRows(row, nRows, false);
        return restoreData;
    }

    internal CellStoreRestoreData RemoveRowAt(int row, int nRows)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.RemoveRowAt(row, nRows);
        restoreData.ValidRestoreData = _validStore.RemoveRowAt(row, nRows);
        restoreData.TypeRestoreData = _typeStore.RemoveRows(row, row + nRows - 1);
        restoreData.FormulaRestoreData = ClearFormulaImpl(new[] { new RowRegion(row, nRows) }).FormulaRestoreData;
        restoreData.FormatRestoreData = _formatStore.RemoveRows(row, row + nRows - 1);
        restoreData.MergeRestoreData = _mergeStore.RemoveRows(row, row + nRows - 1);
        _formulaStore.RemoveRowAt(row, nRows);

        return restoreData;
    }

    internal CellStoreRestoreData RemoveColAt(int col, int nCols)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.RemoveColAt(col, nCols);
        restoreData.ValidRestoreData = _validStore.RemoveColAt(col, nCols);
        restoreData.TypeRestoreData = _typeStore.RemoveCols(col, col + nCols - 1);
        restoreData.FormulaRestoreData = ClearFormulaImpl(new[] { new ColumnRegion(col, nCols) }).FormulaRestoreData;
        restoreData.FormatRestoreData = _formatStore.RemoveCols(col, col + nCols - 1);
        restoreData.MergeRestoreData = _mergeStore.RemoveCols(col, col + nCols - 1);
        _formulaStore.RemoveColAt(col, nCols);

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
            restoreData.Merge(CopyFormula(fromRegion, toRegion));

        if (options.CopyFormat)
            restoreData.Merge(CopyFormatImpl(fromRegion, toRegion));

        _sheet.MarkDirty(toRegion);
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
        var fixedFromRegion = fromRegion.GetIntersection(_sheet.Region) as Region;
        foreach (var position in fixedFromRegion!)
        {
            emptyPalette.Add(new Region(position.row, position.col), _sheet.GetFormat(position.row, position.col));
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
    /// Restores the internal storage state by redoing any actions that caused the internal data to change.
    /// Fires events for the changed data.
    /// </summary>
    /// <param name="restoreData"></param>
    internal void Restore(CellStoreRestoreData restoreData)
    {
        _sheet.BatchUpdates();

        // Set formula through this function so we add the formula back in to the dependency graph
        foreach (var data in restoreData.FormulaRestoreData.DataRemoved)
            this.SetFormulaImpl(data.row, data.col, data.data);

        _validStore.Restore(restoreData.ValidRestoreData);
        _typeStore.Restore(restoreData.TypeRestoreData);
        _dataStore.Restore(restoreData.ValueRestoreData);
        _formatStore.Restore(restoreData.FormatRestoreData);
        _mergeStore.Restore(restoreData.MergeRestoreData);

        foreach (var pt in restoreData.ValueRestoreData.DataRemoved)
        {
            _sheet.MarkDirty(pt.row, pt.col);
            EmitCellChanged(pt.row, pt.col);
        }

        foreach (var region in restoreData.GetAffectedRegions())
        {
            _sheet.MarkDirty(region);
        }

        _sheet.EndBatchUpdates();
    }


    /// <summary>
    /// The <see cref="SheetCell"/> at position row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public SheetCell this[int row, int col]
    {
        get { return new SheetCell(row, col, _sheet); }
    }
}