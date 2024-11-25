using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatashet.Formula.Functions;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.FormulaEngine;

public class FormulaEngine
{
    private readonly Sheet _sheet;
    private readonly CellStore _cells;
    private readonly SheetEnvironment _environment;
    private readonly Parser _parser = new();
    private readonly Evaluator _evaluator;

    /// <summary>
    /// Keeps track of any ranges referenced by formula.
    /// This should ideally keep track of the formula that reference the range also,
    /// but for now it's just whether it's referenced or not.
    /// </summary>
    private readonly RegionDataStore<bool> _observedRanges;

    internal readonly DependencyManager DependencyManager = new();

    public bool IsCalculating { get; private set; }

    public FormulaEngine(Sheet sheet)
    {
        _sheet = sheet;
        _cells = sheet.Cells;
        _sheet.Editor.BeforeCellEdit += SheetOnBeforeCellEdit;
        _cells.CellsChanged += SheetOnCellsChanged;
        _sheet.Rows.Removed += (_, _) => CalculateSheet();
        _sheet.Columns.Removed += (_, _) => CalculateSheet();

        _environment = new SheetEnvironment(sheet);
        _evaluator = new Evaluator(_environment);
        _observedRanges = new RegionDataStore<bool>();

        RegisterDefaultFunctions();
    }

    private void SheetOnCellsChanged(object? sender, CellDataChangedEventArgs e)
    {
        if (this.IsCalculating)
            return;

        var cellsReferenced = false;
        foreach (var cell in e.Positions)
        {
            if (IsCellReferenced(cell.row, cell.col))
            {
                cellsReferenced = true;
                break;
            }
        }

        if (!cellsReferenced)
        {
            foreach (var region in e.Regions)
            {
                if (RegionContainsReferencedCells(region))
                {
                    cellsReferenced = true;
                    break;
                }
            }
        }

        if (!cellsReferenced)
            return;

        this.CalculateSheet();
    }

    private bool IsCellReferenced(int row, int col)
    {
        return DependencyManager.HasDependents(row, col);
    }

    private bool RegionContainsReferencedCells(IRegion region)
    {
        return DependencyManager.HasDependents(region);
    }

    private void RegisterDefaultFunctions()
    {
        _environment.RegisterLogicalFunctions();
        _environment.RegisterMathFunctions();
        _environment.RegisterLookupFunctions();
    }

    private void SheetOnBeforeCellEdit(object? sender, BeforeCellEditEventArgs e)
    {
        var formula = _sheet.Cells.GetFormulaString(e.Cell.Row, e.Cell.Col);
        if (formula != null)
        {
            e.EditValue = formula;
        }
    }

    internal DependencyManagerRestoreData SetFormula(int row, int col, CellFormula? formula)
    {
        return DependencyManager.SetFormula(row, col, formula);
    }

    public CellFormula ParseFormula(string formulaString)
    {
        return _parser.FromString(formulaString);
    }

    public CellValue Evaluate(CellFormula? formula, bool resolveReferences = true)
    {
        if (formula == null)
            return CellValue.Empty;
        try
        {
            return _evaluator.Evaluate(formula, new FormulaExecutionContext(),
                new FormulaEvaluationOptions(!resolveReferences));
        }
        catch (Exception e)
        {
            return CellValue.Error(ErrorType.Na, $"Error running formula: {e.Message}");
        }
    }

    /// <summary>
    /// Removes any vertices that the formula in this cell is dependent on
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal DependencyManagerRestoreData RemoveFormula(int row, int col)
    {
        return DependencyManager.ClearFormula(row, col);
    }

    public IEnumerable<DependencyInfo> GetDependencies() => DependencyManager.GetDependencies();

    public void CalculateSheet()
    {
        if (IsCalculating)
            return;

        IsCalculating = true;
        _sheet.BatchUpdates();

        var order = DependencyManager.GetCalculationOrder();
        var executionContext = new FormulaExecutionContext();

        foreach (var scc in order)
        {
            bool isCircularGroup = false;

            foreach (var vertex in scc)
            {
                if (vertex.Formula == null || vertex.VertexType != VertexType.Cell)
                    continue;

                // if it's part of a scc group, and we don't have circular references, then the value would
                // already have been evaluated.
                CellValue value;

                // To speed up time in scc group, if one vertex is circular the rest will be.
                if (isCircularGroup)
                    value = CellValue.Error(ErrorType.Circular);
                else
                {
                    // check whether the formula has already been calculated in this scc group - may be the case if we lucked
                    // out on the first value calculation and it wasn't a circular reference.
                    if (scc.Count > 1 && executionContext.TryGetExecutedValue(vertex.Formula, out var result))
                    {
                        //TODO: This is never hit so we are always recalculating
                        value = result;
                    }
                    else
                    {
                        value = _evaluator.Evaluate(vertex.Formula, executionContext);
                        if (value.IsError() && ((FormulaError)value.Data!).ErrorType == ErrorType.Circular)
                            isCircularGroup = true;
                    }
                }

                executionContext.ClearExecuting();

                _sheet.Cells.SetValueImpl(vertex.Region!.Top, vertex.Region!.Left, value);
                _sheet.MarkDirty(vertex.Region!.Top, vertex.Region!.Left);
            }
        }

        _sheet.EndBatchUpdates();
        IsCalculating = false;
    }

    /// <summary>
    /// Returns whether a string is a formula - but not necessarily valid.
    /// </summary>
    /// <param name="formula"></param>
    /// <returns></returns>
    public bool IsFormula(string formula)
    {
        return formula.StartsWith('=');
    }

    public void SetVariable(string varName, object value)
    {
        _environment.SetVariable(varName, value);
        CalculateSheet();
    }
}