using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    private Sheet _sheet;

    /// <summary>
    /// Contains cell merge information and handles merges.
    /// </summary>
    public MergeManager Merges { get; }

    /// <summary>
    /// Manages and holds information on cell validators.
    /// </summary>
    public ValidationManager Validation { get; }
    
    public CellStore(Sheet sheet)
    {
        _sheet = sheet;
        Validation = new ValidationManager();
        Merges = new MergeManager(sheet);
    }

    /// <summary>
    /// Returns all cells in the specified region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegion(IRegion region)
    {
        return (new BRange(_sheet, region))
            .Positions
            .Select<(int row, int col), IReadOnlyCell>(x => this.GetCell(x.row, x.col));
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
        return GetCell(position.Row, position.Col);
    }

    internal IEnumerable<(int row, int col)> GetNonEmptyCellPositions(IRegion region)
    {
        return _dataStore.GetNonEmptyPositions(region.TopLeft.Row,
            region.BottomRight.Row,
            region.TopLeft.Col,
            region.BottomRight.Col);
    }

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
        /*var cell = CellDataStore.Get(row, col);
        if (cell == null)
        {
            cell = new Cell();
            CellDataStore.Set(row, col, cell);
        }

        var oldMetaData = cell.GetMetaData(name);

        cell.SetCellMetaData(name, value);
        this.MetaDataChanged?.Invoke(this, new CellMetaDataChangeEventArgs(row, col, name, oldMetaData, value));*/
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
        return GetCell(row, col)?.GetMetaData(name);
    }

    public void SetCell(int row, int col, Cell cell)
    {
        _dataStore.Set(row, col, cell.Data);
        _formatStore.Add(new Region(row, col), cell.Formatting ?? new CellFormat());
        _sheet.MarkDirty(row, col);
    }

    /// <summary>
    /// Clears all cell values in the region
    /// </summary>
    /// <param name="range">The range in which to clear all cells</param>
    public void ClearCells(BRange range)
    {
        var cmd = new ClearCellsCommand(range);
        _sheet.Commands.ExecuteCommand(cmd);
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

        foreach (var pt in restoreData.ValueRestoreData.DataRemoved)
        {
            _sheet.MarkDirty(pt.row, pt.col);
            _sheet.EmitCellChanged(pt.row, pt.col);
        }

        foreach (var region in restoreData.GetAffectedRegions())
        {
            _sheet.MarkDirty(region);
        }

        _sheet.EndBatchUpdates();
    }

    internal CellStoreRestoreData ClearCellsImpl(IEnumerable<IRegion> regionsToClear)
    {
        _sheet.BatchUpdates();
        var restoreData = new CellStoreRestoreData();
        var toClear = regionsToClear.ToList();
        restoreData.ValueRestoreData = _dataStore.Clear(toClear);
        restoreData.ValidRestoreData = _validStore.Clear(toClear);
        restoreData.FormulaRestoreData = ClearFormulaImpl(toClear).FormulaRestoreData;

        var affected = restoreData.GetAffectedPositions().ToList();
        _sheet.EmitCellsChanged(affected);
        _sheet.MarkDirty(affected);

        _sheet.EndBatchUpdates();
        return restoreData;
    }

    internal void InsertColAt(int col, int nCols)
    {
        _dataStore.InsertColAt(col, nCols);
        _formatStore.InsertCols(col, nCols);
        _typeStore.InsertCols(col, nCols);
        _formulaStore.InsertColAt(col, nCols);
        _validStore.InsertColAt(col, nCols);
    }

    internal void InsertRowAt(int row, int nRows)
    {
        _dataStore.InsertRowAt(row, nRows);
        _formatStore.InsertRows(row, nRows);
        _typeStore.InsertRows(row, nRows);
        _formulaStore.InsertRowAt(row, nRows);
        _validStore.InsertRowAt(row, nRows);
    }

    internal CellStoreRestoreData RemoveRowAt(int row, int nRows)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.RemoveRowAt(row, nRows);
        restoreData.ValidRestoreData = _validStore.RemoveRowAt(row, nRows);
        restoreData.TypeRestoreData = _typeStore.RemoveRows(row, row + nRows - 1);
        restoreData.FormulaRestoreData = ClearFormulaImpl(row, nRows).FormulaRestoreData;
        restoreData.FormatRestoreData = _formatStore.RemoveRows(row, row + nRows - 1);
        _formulaStore.RemoveRowAt(row, nRows);

        return restoreData;
    }

    internal CellStoreRestoreData RemoveColAt(int col, int nCols)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.RemoveColAt(col, nCols);
        restoreData.ValidRestoreData = _validStore.RemoveColAt(col, nCols);
        restoreData.TypeRestoreData = _typeStore.RemoveCols(col, col + nCols - 1);
        restoreData.FormulaRestoreData = ClearFormulaImpl(col, nCols).FormulaRestoreData;
        restoreData.FormatRestoreData = _formatStore.RemoveCols(col, col + nCols - 1);
        _formulaStore.RemoveColAt(col, nCols);

        return restoreData;
    }

    public SheetCell this[int row, int col]
    {
        get { return new SheetCell(row, col, _sheet); }
    }
}