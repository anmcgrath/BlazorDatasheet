using System.Collections;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
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

    internal readonly DependencyManager DependencyManager = new();
    public event EventHandler<VariableChangedEventArgs>? VariableChanged;
    public bool IsCalculating { get; private set; }

    /// <summary>
    /// The keys of formula that require recalculation
    /// </summary>
    private readonly HashSet<FormulaVertex> _requiresCalculation = new();

    public FormulaEngine(Sheet sheet)
    {
        _sheet = sheet;
        _cells = sheet.Cells;
        _sheet.Editor.BeforeCellEdit += SheetOnBeforeCellEdit;
        _cells.CellsChanged += SheetOnCellValuesChanged;
        _sheet.Rows.Removed += (_, _) => CalculateSheet();
        _sheet.Columns.Removed += (_, _) => CalculateSheet();

        _environment = new SheetEnvironment(sheet);
        _evaluator = new Evaluator(_environment);

        RegisterDefaultFunctions();
    }

    private void SheetOnCellValuesChanged(object? sender, CellDataChangedEventArgs e)
    {
        if (this.IsCalculating)
            return;

        foreach (var region in e.Positions.Select(x => new Region(x.row, x.col)).Concat(e.Regions))
        {
            var dependents = DependencyManager.GetDependents(region);
            foreach (var dependent in dependents)
                _requiresCalculation.Add(dependent);
        }

        this.Calculate(calculateAll: false);
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
        var restoreData = DependencyManager.SetFormula(row, col, formula);
        _requiresCalculation.Add(new FormulaVertex(row, col, formula));
        Calculate(calculateAll: false);
        return restoreData;
    }

    internal CellFormula ParseFormula(string formulaString)
    {
        return _parser.FromString(formulaString);
    }

    internal CellValue Evaluate(CellFormula? formula, bool resolveReferences = true)
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
        var restoreData = DependencyManager.ClearFormula(row, col);
        foreach (var dependency in DependencyManager.GetDependents(new Region(row, col)))
            _requiresCalculation.Add(dependency);
        Calculate(calculateAll: false);

        return restoreData;
    }

    public void CalculateSheet() => Calculate(calculateAll: true);

    /// <summary>
    /// Calculates the managed formula.
    /// </summary>
    /// <param name="calculateAll">If true, all formula are calculated regardless of whether they require calculation.</param>
    public void Calculate(bool calculateAll)
    {
        if (IsCalculating)
            return;

        if (_requiresCalculation.Count == 0 && !calculateAll)
            return;

        var vertices = calculateAll ? null : _requiresCalculation.ToList();

        IsCalculating = true;
        _sheet.BatchUpdates();

        var order = DependencyManager.GetCalculationOrder(vertices);
        var executionContext = new FormulaExecutionContext();

        foreach (var scc in order)
        {
            bool isCircularGroup = false;

            foreach (var vertex in scc)
            {
                if (vertex.Formula == null ||
                    !(
                        vertex.VertexType != VertexType.Cell || vertex.VertexType != VertexType.Named))
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

                if (vertex.VertexType == VertexType.Cell)
                {
                    _environment.SetCellValue(vertex.Region!.Top, vertex.Region!.Left, value);
                }
                else if (vertex.VertexType == VertexType.Named)
                {
                    var prevValue = _environment.HasVariable(vertex.Key)
                        ? _environment.GetVariable(vertex.Key)
                        : null;
                    VariableChanged?.Invoke(this,
                        new VariableChangedEventArgs(vertex.Key, prevValue, new CellValue(value)));
                    _environment.SetVariable(vertex.Key, value);
                }
            }
        }

        _sheet.EndBatchUpdates();
        IsCalculating = false;
        _requiresCalculation.Clear();
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
        if (value is string s && IsFormula(s))
        {
            var formula = ParseFormula(s);
            DependencyManager.SetFormula(varName, formula);
        }
        else
        {
            var prevValue = _environment.HasVariable(varName) ? _environment.GetVariable(varName) : null;
            VariableChanged?.Invoke(this, new VariableChangedEventArgs(varName, prevValue, new CellValue(value)));
            _environment.SetVariable(varName, new CellValue(value));
        }

        CalculateSheet();
    }

    public CellValue GetVariable(string varName)
    {
        return _environment.GetVariable(varName);
    }

    public void ClearVariable(string varName)
    {
        _environment.ClearVariable(varName);
        DependencyManager.ClearFormula(varName);
        CalculateSheet();
    }

    public IEnumerable<DependencyInfo> GetDependencyInfo() => DependencyManager.GetDependencyInfo();
}