﻿using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.Store;
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
    private readonly SheetEnvironment _environment;
    private readonly Parser _parser = new();
    private readonly Evaluator _evaluator;
    public event EventHandler<VariableChangedEventArgs>? VariableChanged;
    public bool IsCalculating { get; private set; }

    private bool _pauseCalculating;

    /// <summary>
    /// If true, the formula engine will not calculate. When changed to true, recalculation occurs.
    /// </summary>
    public bool PauseCalculating
    {
        get => _pauseCalculating;
        set
        {
            if (value == _pauseCalculating)
                return;

            _pauseCalculating = value;

            if (!_pauseCalculating)
                Calculate(calculateAll: false);
        }
    }

    private readonly DependencyGraph<FormulaVertex> _dependencyGraph = new();

    private readonly RegionDataStore<FormulaVertex> _regionDependencies = new();

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
        _sheet.Cells.FormulaChanged += CellsOnFormulaChanged;
        _sheet.Editor.BeforeCellEdit += EditorOnBeforeCellEdit;
        _sheet.Editor.BeforeEditAccepted += EditorOnBeforeEditAccepted;

        _sheet.Cells.CellsChanged += SheetOnCellValuesChanged;
        _sheet.Rows.Removed += RowsOnRemoved;
        _sheet.Columns.Removed += ColumnsOnRemoved;

        _environment = new SheetEnvironment(sheet);
        _evaluator = new Evaluator(_environment);

        RegisterDefaultFunctions();
    }

    private void CellsOnFormulaChanged(object? sender, CellFormulaChangeEventArgs e)
    {
        CellFormula? parsedFormula = null;
        if (e.NewFormula != null)
            parsedFormula = _parser.FromString(e.NewFormula);

        SetFormula(e.Row, e.Col, parsedFormula);
    }

    private void EditorOnBeforeEditAccepted(object? sender, BeforeAcceptEditEventArgs e)
    {
        if (!IsFormula(e.EditString))
            return;

        var tree = _parser.Parse(e.EditString);
        if (tree.Errors.Count > 0)
        {
            e.AcceptEdit = false;
        }
    }

    private void SheetOnCellValuesChanged(object? sender, CellDataChangedEventArgs e)
    {
        if (this.IsCalculating)
            return;

        PauseCalculating = true;

        foreach (var cell in e.Positions)
        {
            foreach (var u in FindDependentFormula(new Region(cell.row, cell.col)))
                _requiresCalculation.Add(u);
        }

        foreach (var region in e.Regions)
        {
            foreach (var u in FindDependentFormula(region))
                _requiresCalculation.Add(u);
        }

        PauseCalculating = false; // also calculates sheet.
    }

    private void RegisterDefaultFunctions()
    {
        _environment.RegisterLogicalFunctions();
        _environment.RegisterMathFunctions();
        _environment.RegisterLookupFunctions();
    }

    private void EditorOnBeforeCellEdit(object? sender, BeforeCellEditEventArgs e)
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
        AddFormulaVertex(vertex);
        return new FormulaEngineRestoreData();
    }

    private void AddFormulaVertex(FormulaVertex vertex)
    {
        RemoveFormulaVertex(vertex);

        if (vertex.Formula == null)
            return;

        _dependencyGraph.AddVertex(vertex);

        _requiresCalculation.Add(vertex);
        _dirtyReferences.Add(vertex);

        var dependentFormula = FindDependentFormula(vertex); // directly references cell
        if (vertex.Region != null)
            dependentFormula = dependentFormula.Concat(FindDependentFormula(vertex.Region));

        foreach (var v in dependentFormula)
        {
            _requiresCalculation.Add(v);
            _dependencyGraph.AddEdge(vertex, v);
        }

        Calculate(calculateAll: false);
    }

    /// <summary>
    /// Finds the formula that depend on this formula vertx.
    /// </summary>
    /// <param name="vertex"></param>
    /// <returns></returns>
    private IEnumerable<FormulaVertex> FindDependentFormula(FormulaVertex vertex)
    {
        return _dependencyGraph.Adj(vertex);
    }

    private IEnumerable<FormulaVertex> FindDependentFormula(IRegion region)
    {
        return _regionDependencies.GetData(region);
    }

    private List<FormulaVertex> GetVerticesInRegion(IRegion region)
    {
        var vertices = new List<FormulaVertex>();
        foreach (var v in _dependencyGraph.GetAll())
        {
            if (region.Intersects(v.Region))
            {
                vertices.Add(v);
            }
        }

        return vertices;
    }


    /// <summary>
    /// Removes any vertices that the formula in this cell is dependent on
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal FormulaEngineRestoreData RemoveFormula(int row, int col)
    {
        var vertex = _dependencyGraph.GetVertex(GetVertexKey(row, col));
        if (vertex == null)
            return new FormulaEngineRestoreData();

        RemoveFormulaVertex(vertex);

        return new FormulaEngineRestoreData();
    }

    internal void RemoveFormulaVertex(FormulaVertex vertex)
    {
        PauseCalculating = true;

        var dependentFormula = FindDependentFormula(vertex);

        foreach (var v in dependentFormula)
        {
            _requiresCalculation.Add(v);
            _dirtyReferences.Add(v);
        }

        _regionDependencies.Clear(vertex);
        _dependencyGraph.RemoveVertex(vertex, false);

        PauseCalculating = false; // also calculates sheet
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
                    // out on the first value calculation, and it wasn't a circular reference.
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

                if (_dirtyReferences.Contains(vertex))
                    UpdateReferences(vertex, executionContext.GetEvaluatedReferences(vertex.Formula));
            }
        }

        _sheet.EndBatchUpdates();
        IsCalculating = false;

        _requiresCalculation.Clear();
        _dirtyReferences.Clear();
    }

    private void UpdateReferences(FormulaVertex vertex, IEnumerable<Reference> references)
    {
        foreach (var v in _dependencyGraph.Prec(vertex))
            _dependencyGraph.RemoveEdge(v, vertex, false);

        _regionDependencies.Clear(vertex);

        foreach (var r in references)
        {
            if (r.Kind == ReferenceKind.Cell)
            {
                var row = r.Region.Top;
                var col = r.Region.Left;

                var u = _dependencyGraph.GetVertex(GetVertexKey(row, col));
                if (u != null)
                    _dependencyGraph.AddEdge(u, vertex);

                _regionDependencies.Add(r.Region, vertex);
            }

            if (r.Kind == ReferenceKind.Range)
            {
                _regionDependencies.Add(r.Region, vertex);
                foreach (var u in GetVerticesInRegion(r.Region))
                {
                    _dependencyGraph.AddEdge(u, vertex);
                }
            }

            if (r.Kind == ReferenceKind.Named)
            {
                var u = new FormulaVertex(((NamedReference)r).Name, null);
                _dependencyGraph.AddEdge(u, vertex);
            }
        }
    }

    private string GetVertexKey(int row, int col)
    {
        // TODO: get rid of determining keys in formula vertex.
        return (new FormulaVertex(row, col, null)).Key;
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
            AddFormulaVertex(vertex);
        }
        else
        {
            var prevValue = _environment.HasVariable(varName) ? _environment.GetVariable(varName) : null;
            VariableChanged?.Invoke(this, new VariableChangedEventArgs(varName, prevValue, new CellValue(value)));
            _environment.SetVariable(varName, new CellValue(value));
            CalculateSheet();
        }
    }

    public CellValue GetVariable(string varName)
    {
        return _environment.GetVariable(varName);
    }

    public void ClearVariable(string varName)
    {
        _environment.ClearVariable(varName);
        var u = _dependencyGraph.GetVertex(varName);
        if (u != null)
        {
            _dependencyGraph.RemoveVertex(new FormulaVertex(varName, null));
            _regionDependencies.Clear(u);
        }

        CalculateSheet();
    }

    private void ColumnsOnRemoved(object? sender, RowColRemovedEventArgs e)
    {
    }

    private void RowsOnRemoved(object? sender, RowColRemovedEventArgs e)
    {
    }

    /// <summary>
    /// Returns true if the cell has any formula referencing it, or any region that contains it.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    internal bool IsCellReferenced(int row, int col)
    {
        var formulaRefRegion = FindDependentFormula(new Region(row, col));
        return formulaRefRegion.Any();
    }

    public IEnumerable<DependencyInfo> GetDependencyInfo()
    {
        var results = new List<DependencyInfo>();
        foreach (var vertex in _dependencyGraph.GetAll())
        {
            foreach (var dependent in FindDependentFormula(vertex))
            {
                results.Add(new DependencyInfo(dependent.Region!, vertex.Region!, DependencyType.CalculationOrder));
            }
        }

        var dataRegions = _regionDependencies.GetAllDataRegions();
        foreach (var region in dataRegions)
        {
            results.Add(new DependencyInfo(region.Data.Region!, region.Region, DependencyType.Region));
        }

        return results;
    }

    public void Restore(FormulaEngineRestoreData restoreDataFormulaEngineRestoreData)
    {
    }

    /// <summary>
    /// Returns the topological sort of the vertices <paramref name="vertices"/>. If <paramref name="vertices"/> is null, all vertices are considered. Each group of vertices is a strongly connected group.
    /// </summary>
    /// <returns></returns>
    internal IList<IList<FormulaVertex>> GetCalculationOrder(IEnumerable<FormulaVertex>? vertices = null)
    {
        var sort = new SccSort<FormulaVertex>(_dependencyGraph);
        return sort.Sort();
    }
}