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
        var parsed = Sheet.FormulaEngine.ParseFormula(formulaString, Sheet.Name);
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
        restoreData.DependencyManagerRestoreData = Sheet.FormulaEngine.SetFormula(row, col, Sheet.Name, formula);

        FormulaChanged?.Invoke(this,
            new CellFormulaChangeEventArgs(row, col,
                restoreData.FormulaRestoreData.DataRemoved.FirstOrDefault().data,
                formula));
        EmitCellChanged(row, col);
        Sheet.MarkDirty(row, col);
        return restoreData;
    }

    internal CellStoreRestoreData SetFormulaImpl(int row, int col, string formula)
    {
        var parsedFormula = Sheet.FormulaEngine.ParseFormula(formula, Sheet.Name);
        if (parsedFormula.IsValid())
            return SetFormulaImpl(row, col, parsedFormula);
        return new CellStoreRestoreData();
    }

    private CellStoreRestoreData CopyFormulaImpl(Sheet fromSheet, IRegion fromRegion, IRegion toRegion,
        ReferenceAdjustment adjustment)
    {
        var dr = toRegion.Top - fromRegion.Top;
        var dc = toRegion.Left - fromRegion.Left;

        var restoreData = new CellStoreRestoreData();

        // collect before writing: the source may overlap the target
        var formulaToCopy = fromSheet.Cells._formulaStore.GetNonEmptyData(fromRegion).ToList();

        foreach (var formula in formulaToCopy)
        {
            if (formula.data == null || !Sheet.Region.Contains(formula.row + dr, formula.col + dc))
                continue;

            CellFormula clonedFormula;
            if (adjustment == ReferenceAdjustment.Relative)
            {
                // implicit references re-bind to the sheet the formula lands on, as in Excel
                clonedFormula = Sheet.FormulaEngine.CloneFormula(formula.data, Sheet.Name);
                clonedFormula.ShiftReferences(dr, dc, null);
            }
            else
            {
                // the cells move: references into the moved block follow it, everything else stays put
                clonedFormula = Sheet.FormulaEngine.CloneFormula(formula.data, fromSheet.Name);
                clonedFormula.MoveReferencesWithin(fromRegion, fromSheet.Name, dr, dc, Sheet.Name, Sheet.Name);
                clonedFormula.QualifyForeignReferences(Sheet.Name);
            }

            restoreData.Merge(SetFormulaImpl(formula.row + dr, formula.col + dc, clonedFormula));
        }

        return restoreData;
    }

    internal CellStoreRestoreData ClearFormulaImpl(int row, int col)
    {
        return new CellStoreRestoreData()
        {
            FormulaRestoreData = _formulaStore.Clear(row, col),
            DependencyManagerRestoreData = Sheet.FormulaEngine.RemoveFormula(row, col, Sheet.Name)
        };
    }

    internal CellStoreRestoreData ClearFormulaImpl(IEnumerable<IRegion> regions)
    {
        var restoreData = new CellStoreRestoreData();
        var clearedData = _formulaStore.Clear(regions);
        foreach (var clearedFormula in clearedData.DataRemoved)
        {
            restoreData.DependencyManagerRestoreData.Merge(
                Sheet.FormulaEngine.RemoveFormula(clearedFormula.row, clearedFormula.col, Sheet.Name));
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
        Sheet.Commands.ExecuteCommand(new SetParsedFormulaCommand(row, col, parsedFormula));
    }

    internal IMatrixDataStore<CellFormula?> GetFormulaStore() => _formulaStore;
}