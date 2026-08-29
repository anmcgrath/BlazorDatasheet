using System.Diagnostics;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.FormulaEngine;

public class FormulaEngine
{
    private readonly IEnvironment _environment;
    private readonly Parser _parser;
    private readonly Evaluator _evaluator;
    internal readonly DependencyManager DependencyManager = new();
    private readonly List<Sheet> _sheets = new();

    /// <summary>
    /// The formula that require recalculation
    /// </summary>
    private readonly HashSet<FormulaVertex> _requiresCalculation = new();

    private readonly Dictionary<string, PendingVariableChange> _pendingVariableChanges = new();

    /// <summary>
    /// Fired immediately before a non-empty formula recalculation pass begins.
    /// </summary>
    public event EventHandler<CalculationStartedEventArgs>? CalculationStarted;

    /// <summary>
    /// Fired after a formula recalculation pass finishes and sheet updates have been emitted.
    /// </summary>
    public event EventHandler<CalculationCompletedEventArgs>? CalculationCompleted;

    /// <summary>
    /// Fired after a named variable is added, updated, removed, or recalculated.
    /// </summary>
    public event EventHandler<VariableChangedEventArgs>? VariableChanged;

    public FormulaOptions Options { get; private set; }

    public bool IsCalculating { get; private set; }

    internal FormulaEngine(IEnvironment environment, FormulaOptions? options = null)
    {
        Options = options ?? new FormulaOptions();
        _environment = environment;
        _parser = new Parser(_environment, Options);
        _evaluator = new Evaluator(_environment);
    }

    internal void AddSheet(Sheet sheet)
    {
        _sheets.Add(sheet);
        DependencyManager.AddSheet(sheet.Name);
        sheet.Editor.BeforeCellEdit += SheetOnBeforeCellEdit;
        sheet.Cells.CellsChanged += SheetOnCellsChanged;
        sheet.Rows.Removed += RowColsOnRemoved;
        sheet.Columns.Removed += RowColsOnRemoved;
        sheet.Rows.Inserted += RowColsOnInserted;
        sheet.Columns.Inserted += RowColsOnInserted;
    }

    internal void RemoveSheet(Sheet sheet)
    {
        _sheets.Remove(sheet);
        DependencyManager.RemoveSheet(sheet.Name);
        sheet.Editor.BeforeCellEdit -= SheetOnBeforeCellEdit;
        sheet.Cells.CellsChanged -= SheetOnCellsChanged;
        sheet.Rows.Removed -= RowColsOnRemoved;
        sheet.Columns.Removed -= RowColsOnRemoved;
        sheet.Rows.Inserted -= RowColsOnInserted;
        sheet.Columns.Inserted -= RowColsOnInserted;
    }

    private void SheetOnCellsChanged(object? sender, CellDataChangedEventArgs e)
    {
        if (sender is not CellStore cellStore)
            return;

        var sheet = cellStore.Sheet;
        if (this.IsCalculating)
            return;

        foreach (var cell in e.Positions)
        {
            // check if cell itself is a formula vertex, then it should require calculation
            var cellVertex = DependencyManager.GetVertex(cell.row, cell.col, sheet.Name);
            if (cellVertex != null)
            {
                if (!_requiresCalculation.Add(cellVertex))
                    continue;

                foreach (var u in DependencyManager.GetDirectDependents(cellVertex))
                    _requiresCalculation.Add(u);
            }
            else
            {
                foreach (var u in DependencyManager.FindDependentFormula(new Region(cell.row, cell.col), sheet.Name))
                    _requiresCalculation.Add(u);
            }
        }

        foreach (var region in e.Regions)
        {
            foreach (var u in DependencyManager.FindDependentFormula(region, sheet.Name))
                _requiresCalculation.Add(u);
        }

        this.CalculateSheet(false);
    }

    private void RowColsOnRemoved(object? sender, RowColRemovedEventArgs e)
    {
        CalculateSheet(true);
    }

    private void RowColsOnInserted(object? sender, RowColInsertedEventArgs e)
    {
        CalculateSheet(true);
    }

    private void SheetOnBeforeCellEdit(object? sender, BeforeCellEditEventArgs e)
    {
        if (sender is not Editor editor)
            return;

        var sheet = editor.Sheet;
        var formula = sheet.Cells.GetFormulaString(e.Cell.Row, e.Cell.Col);
        if (formula != null)
        {
            e.EditValue = formula;
        }
    }

    public IEnumerable<FunctionDefinition> GetDefinitionsStartingWith(string identifierText) =>
        _environment.SearchForFunctions(identifierText);

    private void QueueVariableForCalculation(string varName, bool includeVariableVertex)
    {
        var variableVertex = DependencyManager.GetVertex(varName) ?? new FormulaVertex(varName, null);
        if (includeVariableVertex)
            _requiresCalculation.Add(variableVertex);

        foreach (var dependent in DependencyManager.GetDirectDependents(variableVertex))
            _requiresCalculation.Add(dependent);
    }

    internal DependencyManagerRestoreData SetFormula(int row, int col, string sheetName, CellFormula? formula)
    {
        return DependencyManager.SetFormula(row, col, sheetName, formula);
    }

    public CellFormula ParseFormula(string formulaString, string callingSheetName, bool useExplicitSheetName = false)
    {
        return _parser.FromString(formulaString, new ParsingContext(callingSheetName, useExplicitSheetName));
    }

    /// <summary>
    /// Evaluates the <paramref name="formula"/> and returns the evaluated value.
    /// </summary>
    /// <param name="formula">The formula string</param>
    /// <param name="callingSheetName">The sheet the formula is called within, controls how references that don't include sheet name are resolved.</param>
    /// <param name="useExplicitSheetName">If true, when serialsied to text, the sheet name is included</param>
    /// <param name="resolveReferences">When true, cell references are resolved to the values, otherwise we return CellValue.Reference. Default is true.</param>
    /// <returns></returns>
    public CellValue EvaluateFormula(string formula, string callingSheetName, bool useExplicitSheetName = false,
        bool resolveReferences = true)
    {
        return EvaluateFormula(ParseFormula(formula, callingSheetName, useExplicitSheetName), resolveReferences);
    }

    /// <summary>
    /// Evaluates the <paramref name="formula"/> and returns the evaluated value
    /// </summary>
    /// <param name="formula"></param>
    /// <param name="resolveReferences">When true, cell references are resolved to the values, otherwise we return CellValue.Reference. Default is true.</param>
    /// <returns></returns>
    public CellValue EvaluateFormula(CellFormula? formula, bool resolveReferences = true)
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
    /// <param name="sheetName"></param>
    internal DependencyManagerRestoreData RemoveFormula(int row, int col, string sheetName)
    {
        return DependencyManager.ClearFormula(row, col, sheetName);
    }

    public IEnumerable<DependencyInfo> GetDependencies() => DependencyManager.GetDependencies();

    public void CalculateSheet(bool calculateAll)
    {
        if (IsCalculating)
            return;

        // asking first means a write to a sheet with no formulas on it does no work at all -
        // otherwise every unbatched cell change pays for a sort over an empty dirty set.
        if (!DependencyManager.HasAnythingToCalculate(calculateAll ? null : _requiresCalculation))
            return;

        var order = DependencyManager.GetCalculationOrder(calculateAll ? null : _requiresCalculation);
        if (order.Count == 0)
            return;

        var formulaCount = order.Sum(group => group.Count(vertex => vertex.Formula is not null));
        var evaluatedFormulaCount = 0;
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        IsCalculating = true;
        var batchedSheets = BeginCalculation();
        try
        {
            CalculationStarted?.Invoke(this, new CalculationStartedEventArgs(calculateAll, formulaCount));
            var executionContext = new FormulaExecutionContext();

            foreach (var scc in order)
                EvaluateStronglyConnectedGroup(scc, executionContext, ref evaluatedFormulaCount);
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            try
            {
                EndCalculation(batchedSheets);
            }
            catch (Exception e)
            {
                exception ??= e;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                EmitPendingVariableChanges();
                CalculationCompleted?.Invoke(this,
                    new CalculationCompletedEventArgs(calculateAll, evaluatedFormulaCount, stopwatch.Elapsed,
                        exception));
            }
        }
    }

    /// <summary>
    /// The sheets batched for the calculation in flight. A snapshot, because a handler reached from
    /// the calculation may add or remove a sheet, and every sheet batched must be unbatched.
    /// Reused rather than reallocated - IsCalculating means only one calculation is ever in flight.
    /// </summary>
    private readonly List<Sheet> _batchedSheets = new();

    private List<Sheet> BeginCalculation()
    {
        _batchedSheets.Clear();
        foreach (var sheet in _sheets)
        {
            sheet.BatchUpdates();
            _batchedSheets.Add(sheet);
        }

        return _batchedSheets;
    }

    private void EndCalculation(List<Sheet> batchedSheets)
    {
        try
        {
            foreach (var sheet in batchedSheets)
                sheet.EndBatchUpdates();
        }
        finally
        {
            _requiresCalculation.Clear();
            IsCalculating = false;
        }
    }

    private void EvaluateStronglyConnectedGroup(IList<FormulaVertex> stronglyConnectedGroup,
        FormulaExecutionContext executionContext, ref int evaluatedFormulaCount)
    {
        bool isCircularGroup = false;

        executionContext.SetCurrentGroup(stronglyConnectedGroup);

        foreach (var vertex in stronglyConnectedGroup)
        {
            var formula = vertex.Formula;
            if (formula == null)
                continue;

            var value = EvaluateFormulaInGroup(vertex, executionContext, ref isCircularGroup);
            executionContext.ClearExecuting();
            ApplyVertexValue(vertex, value);
            evaluatedFormulaCount++;
        }
    }

    private CellValue EvaluateFormulaInGroup(FormulaVertex vertex, FormulaExecutionContext executionContext,
        ref bool isCircularGroup)
    {
        if (isCircularGroup)
            return CellValue.Error(ErrorType.Circular);

        var formula = vertex.Formula!;

        if (executionContext.TryGetExecutedValue(formula, out var cachedValue))
            return cachedValue;

        FormulaCallerInfo? caller = vertex.VertexType == VertexType.Cell
            ? new FormulaCallerInfo(vertex.Row, vertex.Col, vertex.SheetName)
            : null;
        var value = _evaluator.Evaluate(formula, executionContext, caller: caller);
        executionContext.RecordExecuted(formula, value);
        if (value.Data is FormulaError formulaError && formulaError.ErrorType == ErrorType.Circular)
            isCircularGroup = true;
        return value;
    }

    private void ApplyVertexValue(FormulaVertex vertex, CellValue value)
    {
        if (vertex.VertexType == VertexType.Cell)
            _environment.SetCellValue(vertex.Row, vertex.Col, vertex.SheetName, value);
        else if (vertex.VertexType == VertexType.Named)
        {
            QueueVariableChange(vertex.Key.Name, false);
            _environment.SetVariable(vertex.Key.Name, value);
        }
    }

    /// <summary>
    /// Returns whether a string is a formula - but not necessarily valid.
    /// </summary>
    /// <param name="formula"></param>
    /// <returns></returns>
    public static bool IsFormula(string formula)
    {
        return formula.StartsWith('=');
    }

    /// <summary>
    /// Set a variable, if the value is a formula form "=A1" etc. the variable will be set to a formula
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="value"></param>
    /// <exception cref="Exception"></exception>
    public void SetVariable(string varName, object value)
    {
        if (value is string s && IsFormula(s))
        {
            var formula = ParseFormula(s, "");
            if (formula.References.Any(x =>
                    x.Kind != ReferenceKind.Named &&
                    !x.ExplicitSheetName))
            {
                throw new Exception(
                    "Formula references in variables must have explicit sheet names");
            }

            QueueVariableChange(varName, true);
            DependencyManager.SetFormula(varName, formula);
            QueueVariableForCalculation(varName, true);
        }
        else
        {
            QueueVariableChange(varName, true);
            DependencyManager.ClearFormula(varName);
            _environment.SetVariable(varName, new CellValue(value));
            QueueVariableForCalculation(varName, false);
        }

        CalculateSheet(false);
        EmitPendingVariableChangesIfNotCalculating();
    }

    public void SetVariable(string varName, CellValue value)
    {
        QueueVariableChange(varName, true);
        DependencyManager.ClearFormula(varName);
        _environment.SetVariable(varName, value);
        QueueVariableForCalculation(varName, false);
        CalculateSheet(false);
        EmitPendingVariableChangesIfNotCalculating();
    }

    /// <summary>
    /// Returns the variable 
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetVariable(string varName, out CellValue value)
    {
        if (_environment.VariableExists(varName))
        {
            value = _environment.GetVariable(varName);
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Returns the references of the formula stored in the variable <paramref name="varName"/>
    /// </summary>
    /// <param name="varName"></param>
    /// <returns></returns>
    public IEnumerable<Reference> GetVariableReferences(string varName)
    {
        var vertex = DependencyManager.GetVertex(varName);
        if (vertex?.Formula == null)
            return Enumerable.Empty<Reference>();

        return vertex.Formula.References;
    }

    /// <summary>
    /// Returns the references of the formula at <paramref name="row"/>, <paramref name="col"/> in sheet <paramref name="sheetName"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public IEnumerable<Reference> GetReferences(int row, int col, string sheetName)
    {
        var vertex = DependencyManager.GetVertex(row, col, sheetName);
        if (vertex?.Formula == null)
            return Enumerable.Empty<Reference>();
        return vertex.Formula.References;
    }

    /// <summary>
    /// Gives the formula assigned to a variable <paramref name="varName"/>.
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="formula"></param>
    /// <returns></returns>
    public bool TryGetVariableFormula(string varName, out string formula)
    {
        var vertex = DependencyManager.GetVertex(varName);
        if (vertex?.Formula != null)
        {
            formula = vertex.Formula.ToFormulaString();
            return true;
        }

        formula = string.Empty;
        return false;
    }

    internal IEnumerable<Variable> GetVariables()
    {
        foreach (var varName in _environment.GetVariableNames())
        {
            var varValue = _environment.GetVariable(varName);
            var vertex = DependencyManager.GetVertex(varName);
            yield return new Variable(varName, vertex?.Formula?.ToFormulaString(), vertex?.SheetName, varValue);
        }
    }

    public void ClearVariable(string varName)
    {
        QueueVariableChange(varName, true);
        _environment.ClearVariable(varName);
        DependencyManager.ClearFormula(varName);
        CalculateSheet(true);
        EmitPendingVariableChangesIfNotCalculating();
    }

    private void QueueVariableChange(string varName, bool definitionChanged)
    {
        if (_pendingVariableChanges.TryGetValue(varName, out var pendingChange))
        {
            pendingChange.DefinitionChanged |= definitionChanged;
            return;
        }

        _pendingVariableChanges[varName] = new PendingVariableChange(GetVariableState(varName), definitionChanged);
    }

    private VariableState GetVariableState(string varName)
    {
        var hasValue = _environment.TryGetVariable(varName, out var value);
        var formula = DependencyManager.GetVertex(varName)?.Formula?.ToFormulaString();
        return new VariableState(hasValue || formula is not null, hasValue ? value : null, formula);
    }

    private void EmitPendingVariableChangesIfNotCalculating()
    {
        if (!IsCalculating)
            EmitPendingVariableChanges();
    }

    private void EmitPendingVariableChanges()
    {
        if (_pendingVariableChanges.Count == 0)
            return;

        var pendingChanges = _pendingVariableChanges.ToArray();
        _pendingVariableChanges.Clear();

        foreach (var (name, pendingChange) in pendingChanges)
        {
            var newState = GetVariableState(name);
            if (!pendingChange.OldState.Exists && !newState.Exists)
                continue;

            var changeKind = GetVariableChangeKind(pendingChange, newState);
            VariableChanged?.Invoke(this,
                new VariableChangedEventArgs(name, changeKind, pendingChange.OldState.Value, newState.Value,
                    pendingChange.OldState.Formula, newState.Formula));
        }
    }

    private static VariableChangeKind GetVariableChangeKind(PendingVariableChange pendingChange,
        VariableState newState)
    {
        if (!pendingChange.OldState.Exists)
            return VariableChangeKind.Added;
        if (!newState.Exists)
            return VariableChangeKind.Removed;
        return pendingChange.DefinitionChanged ? VariableChangeKind.Updated : VariableChangeKind.Recalculated;
    }

    private sealed class PendingVariableChange(VariableState oldState, bool definitionChanged)
    {
        public VariableState OldState { get; } = oldState;
        public bool DefinitionChanged { get; set; } = definitionChanged;
    }

    private readonly record struct VariableState(bool Exists, CellValue? Value, string? Formula);

    internal void RenameSheet(string oldName, string newName)
    {
        DependencyManager.RenameSheet(oldName, newName);
    }

    internal IEnvironment GetEnvironment()
    {
        return _environment;
    }

    internal CellFormula CloneFormula(CellFormula formula)
    {
        return _parser.FromString(formula.ToFormulaString());
    }

    /// <summary>
    /// Returns the registered function with name <paramref name="functionName"/>, or null if not registered.
    /// </summary>
    public FunctionDescriptor? GetFunction(string functionName)
    {
        return _environment.TryGetFunction(functionName, out var function) ? function : null;
    }
}