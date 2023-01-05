using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Sheet;
using BlazorDatasheet.FormulaEngine.Graph;
using BlazorDatasheet.FormulaEngine.Interpreter;
using BlazorDatasheet.FormulaEngine.Interpreter.References;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;
using ExpressionEvaluator.CodeAnalysis.Types;

namespace BlazorDatasheet.FormulaEngine;

public class FormulaEngine
{
    private readonly ISheet _sheet;
    private Environment _environment;
    private readonly Parser _parser = new Parser();
    private readonly Lexer _lexer = new Lexer();
    private readonly FormulaEvaluator _evaluator;
    private readonly Dictionary<(int row, int col), Formula> _formula = new();
    private readonly DependencyGraph _dependencyGraph;

    public FormulaEngine(ISheet sheet)
    {
        _sheet = sheet;
        _environment = new Environment(sheet);
        _evaluator = new FormulaEvaluator(_environment);
        _dependencyGraph = new DependencyGraph();
    }

    public object Evaluate(int row, int col)
    {
        if (!_formula.ContainsKey((row, col)))
            return null;
        return _evaluator.Evaluate(_formula[(row, col)].ExpressionTree);
    }

    public Formula Parse(string formulaText)
    {
        return new Formula(_parser.Parse(_lexer, formulaText));
    }

    public void SetFormula(int row, int col, string formulaString)
    {
        // get the formula & remove the equals at the beginning
        var formula = Parse(formulaString.Substring(1, formulaString.Length - 1));
        var exists = _formula.ContainsKey((row, col));
        if (!exists)
            _formula.Add((row, col), formula);
        else
        {
            _dependencyGraph.RemoveVertex(new CellVertex(row, col));
            _formula[(row, col)] = formula;
        }

        var formulaVertex = new CellVertex(row, col);
        _dependencyGraph.AddVertex(formulaVertex);
        _dependencyGraph.AddEdges(formula.References.Select(GetVertex), formulaVertex);

        // For now, recompute the whole sheet... later will be smarter about it
        CalculateSheet();
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
                    if (value is FormulaError formulaError)
                        value = formulaError.Message;

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