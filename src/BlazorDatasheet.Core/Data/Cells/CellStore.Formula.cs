using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    /// <summary>
    /// Cell FORMULA
    /// </summary>
    private readonly IMatrixDataStore<CellFormula?> _formulaStore = new SparseMatrixStoreByRows<CellFormula?>();

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

        if (!restoreData.ValueRestoreData.DataRemoved.Any())
            restoreData.ValueRestoreData.DataRemoved.Add((row, col, new CellValue(null)));

        restoreData.FormulaRestoreData = _formulaStore.Set(row, col, formula);
        restoreData.DependencyManagerRestoreData = _sheet.FormulaEngine.SetFormula(row, col, formula);

        FormulaChanged?.Invoke(this,
            new CellFormulaChangeEventArgs(row, col,
                restoreData.FormulaRestoreData.DataRemoved.FirstOrDefault().data,
                formula));
        EmitCellChanged(row, col);
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

    private CellStoreRestoreData CopyFormulaImpl(IRegion fromRegion, IRegion toRegion)
    {
        var offset = new CellPosition(
            toRegion.TopLeft.row - fromRegion.TopLeft.row,
            toRegion.TopLeft.col - fromRegion.TopLeft.col);

        var restoreData = new CellStoreRestoreData();

        var formulaToCopy = _formulaStore.GetNonEmptyData(fromRegion);

        foreach (var formula in formulaToCopy)
        {
            if (formula.data == null)
                continue;
            var clonedFormula = formula.data.Clone();
            clonedFormula.ShiftReferences(offset.row, offset.col);
            restoreData.Merge(SetFormulaImpl(formula.row + offset.row, formula.col + offset.col, clonedFormula));
        }

        return restoreData;
    }

    internal CellStoreRestoreData ClearFormulaImpl(int row, int col)
    {
        return new CellStoreRestoreData()
        {
            FormulaRestoreData = _formulaStore.Clear(row, col),
            DependencyManagerRestoreData = _sheet.FormulaEngine.RemoveFormula(row, col)
        };
    }

    internal CellStoreRestoreData ClearFormulaImpl(IEnumerable<IRegion> regions)
    {
        var restoreData = new CellStoreRestoreData();
        var clearedData = _formulaStore.Clear(regions);
        foreach (var clearedFormula in clearedData.DataRemoved)
        {
            restoreData.DependencyManagerRestoreData.Merge(
                _sheet.FormulaEngine.RemoveFormula(clearedFormula.row, clearedFormula.col));
        }

        restoreData.FormulaRestoreData = clearedData;
        return restoreData;
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

    internal IMatrixDataStore<CellFormula?> GetFormulaStore() => _formulaStore;
}