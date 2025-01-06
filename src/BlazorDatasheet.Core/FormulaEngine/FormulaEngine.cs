using System.Collections;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
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
    public event EventHandler<VariableChangedEventArgs>? VariableChanged;
    public bool IsCalculating { get; private set; }

    private readonly DependencyGraph<FormulaVertex> _dependencyGraph = new();

    /// <summary>
    /// The formula that require recalculation
    /// </summary>
    private readonly HashSet<FormulaVertex> _requiresCalculation = new();

    /// <summary>
    /// Formula that have dirty references.
    /// This could happen if
    /// 1. formula references volatile function
    /// 2. reference a named region and the named region changes
    /// 3. rows/cols are inserted/deleted
    /// 4. the formula changes.
    /// </summary>
    private readonly HashSet<FormulaVertex> _dirtyReferences = new();

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

        this.Calculate(calculateAll: true);
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

    internal FormulaEngineRestoreData SetFormula(int row, int col, CellFormula? formula)
    {
        var vertex = new FormulaVertex(row, col, formula);
        _dependencyGraph.AddVertex(vertex);

        _requiresCalculation.Add(vertex);
        _dirtyReferences.Add(vertex);

        Calculate(calculateAll: false);
        return new FormulaEngineRestoreData();
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
    internal FormulaEngineRestoreData RemoveFormula(int row, int col)
    {
        _dependencyGraph.RemoveVertex(new FormulaVertex(row, col, null), true);
        Calculate(calculateAll: true);
        return new FormulaEngineRestoreData();
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

        var order = GetCalculationOrder(vertices);
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
        _dirtyReferences.Clear();
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
            var vertex = new FormulaVertex(varName, formula);
            _requiresCalculation.Add(vertex);
            _dirtyReferences.Add(vertex);
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
        _dirtyReferences.Add(new FormulaVertex(varName, null));

        CalculateSheet();
    }

    public IEnumerable<DependencyInfo> GetDependencyInfo() => Array.Empty<DependencyInfo>();

    public FormulaEngineRestoreData RemoveRowColAt(int index, int count, Axis axis)
    {
        return new FormulaEngineRestoreData();
    }

    public FormulaEngineRestoreData InsertRowColAt(int index, int count, Axis axis)
    {
        return new FormulaEngineRestoreData();
    }

    public void Restore(FormulaEngineRestoreData restoreDataFormulaEngineRestoreData)
    {
    }

    /// <summary>
    /// Returns the topological sort of the vertices <paramref name="vertices"/>. If <paramref name="vertices"/> is null, all vertices are considered. Each group of vertices is a strongly connected group.
    /// </summary>
    /// <returns></returns>
    private IList<IList<FormulaVertex>> GetCalculationOrder(IEnumerable<FormulaVertex>? vertices = null)
    {
        var sort = new SccSort<FormulaVertex>(_dependencyGraph);
        return sort.Sort();
    }
}