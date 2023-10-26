using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    /// <summary>
    /// Cell FORMULA
    /// </summary>
    private readonly IMatrixDataStore<CellFormula?> _formulaStore = new SparseMatrixStore<CellFormula?>();

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
    /// Sets the parsed formula for a cell. Adds the formula to the engine's dependency graph.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="formula"></param>
    /// <returns></returns>
    internal CellStoreRestoreData SetFormulaImpl(int row, int col, CellFormula? formula)
    {
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
}