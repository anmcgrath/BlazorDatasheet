using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Events.Edit;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.FormulaEngine;

public class FormulaEngine
{
    private readonly Sheet _sheet;
    private IEnvironment _environment;
    private readonly FormulaParser _parser = new();
    private readonly FormulaEvaluator _evaluator;
    private readonly Dictionary<(int row, int col), CellFormula> _formula = new();
    private readonly DependencyGraph _dependencyGraph;
    private bool _isCalculating = false;

    public FormulaEngine(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.CellsChanged += SheetOnCellsChanged;
        _sheet.Editor.BeforeEditAccepted += SheetOnBeforeEditAccepted;
        _sheet.Editor.EditAccepted += SheetOnEditAccepted;
        _sheet.Editor.BeforeCellEdit += SheetOnBeforeCellEdit;

        _environment = new SheetEnvironment(sheet);
        _evaluator = new FormulaEvaluator(_environment);
        _dependencyGraph = new DependencyGraph();
    }

    private void SheetOnBeforeCellEdit(object? sender, BeforeCellEditEventArgs e)
    {
        if (this.HasFormula(e.Cell))
        {
            e.EditValue = this.GetFormulaString(e.Cell);
        }
    }

    private void SheetOnEditAccepted(object? sender, EditAcceptedEventArgs e)
    {
    }

    public bool HasFormula(int row, int col)
    {
        return this.HasFormula(_sheet.GetCell(row, col));
    }

    public bool HasFormula(IReadOnlyCell cell)
    {
        return _formula.ContainsKey((cell.Row, cell.Col));
    }

    /// <summary>
    /// Returns the formula string for a cell, if it has it. If it has no formula, returns null.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public string? GetFormulaString(int row, int col)
    {
        return GetFormulaString(row, col);
    }

    /// <summary>
    /// Returns the formula string for a cell, if it has it. If it has no formula, returns null.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public string? GetFormulaString(IReadOnlyCell cell)
    {
        if (HasFormula(cell))
            return _formula[(cell.Row, cell.Col)].ToFormulaString();

        return null;
    }

    private void SheetOnBeforeEditAccepted(object? sender, BeforeAcceptEditEventArgs e)
    {
        var editor = (Editor)sender!;
        if (e.EditValue is string f)
        {
            if (IsFormula(f))
            {
                // Don't let the sheet set the value,
                // we set a formula and compute it.
                e.AcceptEdit = false;
                if (ParseAndSetFormula(e.Cell.Row, e.Cell.Col, f))
                {
                    e.StopEditing();
                }
            }
        }
    }

    /// <summary>
    /// Sets the formula for a cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="formulaString">The formula string. Must start with =</param>
    /// <returns>True if the formula was valid and was set, false otherwise.</returns>
    public bool SetFormulaString(int row, int col, string formulaString)
    {
        if (IsFormula(formulaString))
        {
            return ParseAndSetFormula(row, col, formulaString);
        }

        return false;
    }

    private void SheetOnCellsChanged(object? sender, IEnumerable<ChangeEventArgs> e)
    {
        if (_isCalculating)
            return;

        // If cell values are being set while the formula engine is not calculating,
        // then these values must override the formula and so the formula should be cleared
        // at those cell positions.
        foreach (var change in e)
        {
            this.RemoveFormula(change.Row, change.Col);
        }

        CalculateSheet();
    }

    /// <summary>
    /// Parses and sets a cell formula. Returns false if the formula is invalid.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="formulaString"></param>
    /// <returns></returns>
    private bool ParseAndSetFormula(int row, int col, string formulaString)
    {
        var formula = _parser.FromString(formulaString);
        if (!formula.IsValid())
            return false;

        SetFormula(row, col, formula);

        return true;
    }

    public void SetFormula(int row, int col, CellFormula formula)
    {
        var exists = _formula.ContainsKey((row, col));
        if (!exists)
            _formula.Add((row, col), formula);
        else
        {
            _formula[(row, col)] = formula;
        }

        var formulaVertex = new CellVertex(row, col);
        _dependencyGraph.AddVertex(formulaVertex);
        _dependencyGraph.AddEdges(formula.References!.Select(GetVertex), formulaVertex);
    }

    public CellFormula ParseFormula(string formulaString)
    {
        return _parser.FromString(formulaString);
    }

    public object? Evaluate(int row, int col)
    {
        if (!_formula.ContainsKey((row, col)))
            return null;
        return _evaluator.Evaluate(_formula[(row, col)]);
    }

    public object? Evaluate(CellFormula? formula)
    {
        if (formula == null)
            return null;
        return _evaluator.Evaluate(formula);
    }

    /// <summary>
    /// Removes a formula and recalculates the sheet.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void ClearFormula(int row, int col)
    {
        RemoveFormula(row, col);
        CalculateSheet();
    }

    /// <summary>
    /// Deletes a formula if it exists.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    private void RemoveFormula(int row, int col)
    {
        if (HasFormula(row, col))
        {
            _formula.Remove((row, col));
            _dependencyGraph.RemoveVertex(new CellVertex(row, col));
        }
    }

    public void CalculateSheet()
    {
        // Sheet.Pause();
        // Stop the sheet from emitting events
        // Sheet.Resume(); should do a bulk event dispatch
        // So that the renderer can handle the updated cells...
        _isCalculating = true;

        var order =
            _dependencyGraph
                .TopologicalSort();

        foreach (var vertex in order)
        {
            if (vertex is CellVertex cellVertex)
            {
                if (HasFormula(cellVertex.Row, cellVertex.Col))
                {
                    var value = this.Evaluate(cellVertex.Row, cellVertex.Col);
                    _sheet.TrySetCellValueImpl(cellVertex.Row, cellVertex.Col, value, true);
                }
            }
        }

        _isCalculating = false;
    }

    private Vertex GetVertex(Reference reference)
    {
        if (reference is CellReference cellReference)
            return new CellVertex(cellReference.Row.RowNumber, cellReference.Col.ColNumber);

        throw new Exception("Could not convert reference to vertex");
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
}