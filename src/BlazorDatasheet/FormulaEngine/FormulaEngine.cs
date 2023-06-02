using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Events;
using BlazorDatasheet.FormulaEngine.Interpreter.References;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine;

public class FormulaEngine
{
    private readonly Sheet _sheet;
    private IEnvironment _environment;
    private readonly FormulaParser _parser = new();
    private readonly FormulaEvaluator _evaluator;
    private readonly Dictionary<(int row, int col), Formula> _formula = new();
    private readonly DependencyGraph _dependencyGraph;
    private bool _isCalculating = false;

    public FormulaEngine(IEnvironment environment)
    {
        _sheet.FormulaChanged += SheetOnFormulaChanged;
        _sheet.CellsChanged += SheetOnCellsChanged;
        _environment = environment;
        _evaluator = new FormulaEvaluator(_environment);
        _dependencyGraph = new DependencyGraph();
    }

    private void SheetOnCellsChanged(object? sender, IEnumerable<ChangeEventArgs> e)
    {
        if (_isCalculating)
            return;

        _isCalculating = true;
        CalculateSheet();
        _isCalculating = false;
    }

    private void SheetOnFormulaChanged(object? sender, FormulaChangedEventArgs e)
    {
        var row = e.Row;
        var col = e.Col;

        if (e.NewFormula == null)
        {
            ClearFormula(row, col);
        }
        else
        {
            var formula = _parser.FromString(e.NewFormula);

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

        // For now, recompute the whole sheet... later will be smarter about it
        CalculateSheet();
    }

    public object Evaluate(int row, int col)
    {
        if (!_formula.ContainsKey((row, col)))
            return null;
        return _evaluator.Evaluate(_formula[(row, col)].ExpressionTree);
    }

    public void ClearFormula(int row, int col)
    {
        _formula.Remove((row, col));
        _dependencyGraph.RemoveVertex(new CellVertex(row, col));
    }

    public void CalculateSheet()
    {
        // Sheet.Pause();
        // Stop the sheet from emitting events
        // Sheet.Resume(); should do a bulk event dispatch
        // So that the renderer can handle the updated cells...

        var order =
            _dependencyGraph
                .TopologicalSort();

        foreach (var vertex in order)
        {
            if (vertex is CellVertex cellVertex)
            {
                if (_formula.ContainsKey((cellVertex.Row, cellVertex.Col)))
                {
                    var value = this.Evaluate(cellVertex.Row, cellVertex.Col);
                    _sheet.TrySetCellValue(cellVertex.Row, cellVertex.Col, value);
                }
            }
        }
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