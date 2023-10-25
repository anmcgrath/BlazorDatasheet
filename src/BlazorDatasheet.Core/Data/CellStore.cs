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
    internal readonly IMatrixDataStore<object?> CellDataStore = new SparseMatrixStore<object?>();

    /// <summary>
    /// Cell FORMULA
    /// </summary>
    internal readonly IMatrixDataStore<CellFormula?> CellFormulaStore = new SparseMatrixStore<CellFormula?>();

    /// <summary>
    /// Stores whether cells are valid.
    /// </summary>
    internal readonly IMatrixDataStore<bool> ValidationStore = new SparseMatrixStore<bool>();

    /// <summary>
    /// Stores individual cell formats.
    /// </summary>
    internal readonly IMatrixDataStore<CellFormat?> CellFormatStore = new SparseMatrixStore<CellFormat?>();

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
        return CellDataStore.GetNonEmptyPositions(region.TopLeft.Row,
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
    /// Sets cell values to those specified.
    /// </summary>
    /// <param name="changes"></param>
    /// <returns></returns>
    public bool SetCellValues(IEnumerable<(int row, int col, object value)> changes)
    {
        _sheet.BatchDirty();
        _sheet.Commands.BeginCommandGroup();
        foreach (var change in changes)
        {
            _sheet.Commands.ExecuteCommand(new SetCellValueCommand(change.row, change.col, change.value));
        }

        _sheet.Commands.EndCommandGroup();
        _sheet.EndBatchDirty();

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
        CellDataStore.Set(row, col, cell.Data);
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
        return CellDataStore.Get(row, col);
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

    /// <summary>
    /// Whether the cell has a formula set.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool HasFormula(int row, int col)
    {
        return CellFormulaStore.Get(row, col) != null;
    }

    public string? GetFormulaString(int row, int col)
    {
        return CellFormulaStore.Get(row, col)?.ToFormulaString();
    }

    /// <summary>
    /// Sets the formula for a row and col, and calculate the sheet.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="parsedFormula"></param>
    public void SetFormula(int row, int col, CellFormula parsedFormula)
    {
        _sheet.Commands.ExecuteCommand(new SetParsedFormulaCommand(row, col, parsedFormula, true));
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
        var cellsAffected = CellDataStore.GetNonEmptyPositions(region).ToList();
        foreach (var (row, col) in cellsAffected)
        {
            var cellData = CellDataStore.Get(row, col);
            var result = Validation.Validate(cellData, row, col);
            ValidationStore.Set(row, col, result.IsValid);
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
    }
}