using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data.Restore;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data;

public class CellStore
{
    private Sheet _sheet;

    /// <summary>
    /// The cell DATA
    /// </summary>
    private readonly IMatrixDataStore<object?> _dataStore = new SparseMatrixStore<object?>();

    /// <summary>
    /// Cell FORMULA
    /// </summary>
    private readonly IMatrixDataStore<CellFormula?> _formulaStore = new SparseMatrixStore<CellFormula?>();

    /// <summary>
    /// Stores whether cells are valid.
    /// </summary>
    private readonly IMatrixDataStore<bool> _validStore = new SparseMatrixStore<bool>();

    /// <summary>
    /// Stores individual cell formats.
    /// </summary>
    internal readonly IMatrixDataStore<CellFormat?> CellFormatStore = new SparseMatrixStore<CellFormat?>();

    private readonly ConsolidatedDataStore<string> _typeStore = new();

    public CellStore(Sheet sheet)
    {
        _sheet = sheet;
        Validation = new ValidationManager();
        Merges = new MergeManager(sheet);
    }

    /// <summary>
    /// Contains cell merge information and handles merges.
    /// </summary>
    public MergeManager Merges { get; }

    /// <summary>
    /// Manages and holds information on cell validators.
    /// </summary>
    public ValidationManager Validation { get; }

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
    /// Sets the cell value using <see cref="SetCellValueCommand"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetValue(int row, int col, object value)
    {
        var cmd = new SetCellValueCommand(row, col, value);
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets a cell value to the value specified and raises the appropriate events.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns>Restore data that stores the changes made.</returns>
    internal CellStoreRestoreData SetValueImpl(int row, int col, object value)
    {
        var restoreData = new CellStoreRestoreData();

        // If cell values are being set while the formula engine is not calculating,
        // then these values must override the formula and so the formula should be cleared
        // at those cell positions.
        if (!_sheet.FormulaEngine.IsCalculating && HasFormula(row, col))
        {
            restoreData.FormulaRestoreData = ClearFormulaImpl(row, col).FormulaRestoreData;
        }

        // Validate but don't stop setting cell values if the value is invalid.
        var validationResult = Validation.Validate(value, row, col);

        // Save old validation result and current cell values.
        restoreData.ValidRestoreData = _validStore.Set(row, col, validationResult.IsValid);
        restoreData.ValueRestoreData = _dataStore.Set(row, col, value);

        _sheet.EmitCellChanged(row, col);
        _sheet.MarkDirty(row, col);

        return restoreData;
    }

    /// <summary>
    /// Sets cell values to those specified using <see cref="SetCellValueCommand"/>
    /// </summary>
    /// <param name="changes"></param>
    /// <returns></returns>
    public bool SetValues(IEnumerable<(int row, int col, object value)> changes)
    {
        _sheet.BatchUpdates();
        _sheet.Commands.BeginCommandGroup();
        foreach (var change in changes)
        {
            _sheet.Commands.ExecuteCommand(new SetCellValueCommand(change.row, change.col, change.value));
        }

        _sheet.Commands.EndCommandGroup();
        _sheet.EndBatchUpdates();

        return true;
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
        CellFormatStore.Set(row, col, cell.Formatting);
        _sheet.MarkDirty(row, col);
    }

    /// <summary>
    /// Gets the cell's value at row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public object? GetValue(int row, int col)
    {
        return _dataStore.Get(row, col);
    }

    /// <summary>
    /// Set the formula string for a row and col, and calculate the sheet.
    /// If the parsed formula is invalid, the formula will not be set.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="formulaString"></param>
    public void SetFormula(int row, int col, string formulaString)
    {
        var parsed = _sheet.FormulaEngine.ParseFormula(formulaString);
        if (parsed.IsValid())
            SetFormula(row, col, parsed);
    }

    internal CellStoreRestoreData SetFormulaImpl(int row, int col, CellFormula? formula)
    {
        // Clear existing values, and save the previous value
        var restoreData = new CellStoreRestoreData();
        restoreData.ValidRestoreData = _validStore.Clear(row, col);
        restoreData.ValueRestoreData = _dataStore.Clear(row, col);
        restoreData.FormulaRestoreData = _formulaStore.Set(row, col, formula);
        if (formula != null)
            _sheet.FormulaEngine.AddToDependencyGraph(row, col, formula);
        _sheet.EmitCellChanged(row, col);
        _sheet.MarkDirty(row, col);
        return restoreData;
    }

    internal CellStoreRestoreData SetFormulaImpl(int row, int col, string formula)
    {
        var parsedFormula = _sheet.FormulaEngine.ParseFormula(formula);
        if (parsedFormula.IsValid())
            return SetFormulaImpl(row, col, parsedFormula);
        return new CellStoreRestoreData();
    }

    internal CellStoreRestoreData ClearFormulaImpl(int row, int col)
    {
        var restoreData = _formulaStore.Clear(row, col);
        _sheet.FormulaEngine.RemoveFromDependencyGraph(row, col);
        return new CellStoreRestoreData() { FormulaRestoreData = restoreData };
    }

    internal CellStoreRestoreData ClearFormulaImpl(IEnumerable<IRegion> regions)
    {
        var clearedData = _formulaStore.Clear(regions);
        foreach (var clearedFormula in clearedData.DataRemoved)
        {
            _sheet.FormulaEngine.RemoveFromDependencyGraph(clearedFormula.row, clearedFormula.col);
        }

        return new CellStoreRestoreData()
        {
            FormulaRestoreData = clearedData
        };
    }

    /// <summary>
    /// Whether the cell has a formula set.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool HasFormula(int row, int col)
    {
        return _formulaStore.Get(row, col) != null;
    }

    public string? GetFormulaString(int row, int col)
    {
        return _formulaStore.Get(row, col)?.ToFormulaString();
    }

    public CellFormula? GetFormula(int row, int col)
    {
        return _formulaStore.Get(row, col);
    }

    /// <summary>
    /// Sets the formula for a row and col, and calculate the sheet.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="parsedFormula"></param>
    public void SetFormula(int row, int col, CellFormula parsedFormula)
    {
        _sheet.Commands.ExecuteCommand(new SetParsedFormulaCommand(row, col, parsedFormula));
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

    internal void ValidateRegion(IRegion region)
    {
        var cellsAffected = _dataStore.GetNonEmptyPositions(region).ToList();
        foreach (var (row, col) in cellsAffected)
        {
            var cellData = _dataStore.Get(row, col);
            var result = Validation.Validate(cellData, row, col);
            _validStore.Set(row, col, result.IsValid);
        }

        _sheet.MarkDirty(cellsAffected);
    }

    /// <summary>
    /// Add a <see cref="IDataValidator"> to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validator"></param>
    public void AddValidator(IRegion region, IDataValidator validator)
    {
        var cmd = new SetValidatorCommand(region, validator);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Adds multiple validators to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validators"></param>
    public void AddValidators(IRegion region, IEnumerable<IDataValidator> validators)
    {
        _sheet.Commands.BeginCommandGroup();
        foreach (var validator in validators)
        {
            AddValidator(region, validator);
        }

        _sheet.Commands.EndCommandGroup();
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

    public string GetCellType(int row, int col)
    {
        var type = _typeStore.Get(row, col);
        return type ?? "text";
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
        CellFormatStore.InsertColAt(col, nCols);
        _typeStore.InsertCols(col, col + nCols - 1);
        _formulaStore.InsertColAt(col, nCols);
        _validStore.InsertColAt(col, nCols);
    }

    internal void InsertRowAt(int row, int nRows)
    {
        _dataStore.InsertRowAt(row, nRows);
        CellFormatStore.InsertRowAt(row, nRows);
        _typeStore.InsertRows(row, row + nRows - 1);
        _formulaStore.InsertRowAt(row, nRows);
        _validStore.InsertRowAt(row, nRows);
    }

    internal CellStoreRestoreData RemoveRowAt(int row, int nRows)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.RemoveRowAt(row, nRows);
        restoreData.ValidRestoreData = _validStore.RemoveRowAt(row, nRows);
        restoreData.TypeRestoreData = _typeStore.RemoveRows(row, row + nRows - 1);

        // TODO : NOT CORRECT!!
        restoreData.FormulaRestoreData = ClearFormulaImpl(row, nRows).FormulaRestoreData;
        _formulaStore.RemoveRowAt(row, nRows);

        return restoreData;
    }

    internal CellStoreRestoreData RemoveColAt(int col, int nCols)
    {
        var restoreData = new CellStoreRestoreData();
        restoreData.ValueRestoreData = _dataStore.RemoveColAt(col, nCols);
        restoreData.ValidRestoreData = _validStore.RemoveColAt(col, nCols);
        restoreData.TypeRestoreData = _typeStore.RemoveCols(col, col + nCols - 1);

        // TODO : NOT CORRECT!!
        restoreData.FormulaRestoreData = ClearFormulaImpl(col, nCols).FormulaRestoreData;
        _formulaStore.RemoveColAt(col, nCols);

        return restoreData;
    }

    public SheetCell this[int row, int col]
    {
        get { return new SheetCell(row, col, _sheet); }
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
        return restoreData;
    }

    public void SetCellType(IRegion region, string type)
    {
        _sheet.Commands.ExecuteCommand(new SetTypeCommand(region, type));
    }
}