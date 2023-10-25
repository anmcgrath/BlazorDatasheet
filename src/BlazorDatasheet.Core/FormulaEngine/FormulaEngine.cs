using System.Diagnostics;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.CoreFunctions;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.FormulaEngine;

public class FormulaEngine
{
    private readonly Sheet _sheet;
    private SheetEnvironment _environment;
    private readonly FormulaParser _parser = new();
    private readonly FormulaEvaluator _evaluator;
    private readonly DependencyGraph _dependencyGraph;
    public bool IsCalculating { get; private set; }

    public FormulaEngine(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.Editor.BeforeCellEdit += SheetOnBeforeCellEdit;

        _environment = new SheetEnvironment(sheet);
        _evaluator = new FormulaEvaluator(_environment);
        _dependencyGraph = new DependencyGraph();

        RegisterDefaultFunctions();
    }

    private void RegisterDefaultFunctions()
    {
        _environment.SetFunction("IF", new IfFunction());
        _environment.SetFunction("SIN", new SinFunction());
    }

    private void SheetOnBeforeCellEdit(object? sender, BeforeCellEditEventArgs e)
    {
        var formula = _sheet.Cells.CellFormulaStore.Get(e.Cell.Row, e.Cell.Col);
        if (formula != null)
        {
            e.EditValue = formula.ToFormulaString();
        }
    }

    public void AddToDependencyGraph(int row, int col, CellFormula formula)
    {
        var formulaVertex = new CellVertex(row, col);
        _dependencyGraph.AddVertex(formulaVertex);
        _dependencyGraph.AddEdges(formula.References!.Select(GetVertex), formulaVertex);
    }

    public CellFormula ParseFormula(string formulaString)
    {
        return _parser.FromString(formulaString);
    }

    public object? Evaluate(CellFormula? formula)
    {
        if (formula == null)
            return null;
        return _evaluator.Evaluate(formula);
    }

    /// <summary>
    /// Deletes a formula if it exists.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void RemoveFromDependencyGraph(int row, int col)
    {
        _dependencyGraph.RemoveVertex(new CellVertex(row, col));
    }

    public void CalculateSheet()
    {
        if (IsCalculating)
            return;
        
        Console.WriteLine("CALCULATING");

        IsCalculating = true;
        _sheet.BatchDirty();

        var order =
            _dependencyGraph
                .TopologicalSort();

        var changedValuePositions = new List<(int row, int col)>();

        foreach (var vertex in order)
        {
            if (vertex is CellVertex cellVertex)
            {
                var formula = _sheet.Cells.CellFormulaStore.Get(cellVertex.Row, cellVertex.Col);
                if (formula != null)
                {
                    var value = this.Evaluate(formula);
                    _sheet.Cells.CellDataStore.Set(cellVertex.Row, cellVertex.Col, value);
                    _sheet.MarkDirty(cellVertex.Row, cellVertex.Col);
                    changedValuePositions.Add((cellVertex.Row, cellVertex.Col));
                }
            }
        }

        _sheet.EmitCellsChanged(changedValuePositions);
        _sheet.EndBatchDirty();

        IsCalculating = false;
    }

    private Vertex GetVertex(Reference reference)
    {
        if (reference is CellReference cellReference)
            return new CellVertex(cellReference.Row.RowNumber, cellReference.Col.ColNumber);
        if (reference is NamedReference namedReference)
        {
            return new NamedVertex(namedReference.Name);
        }

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

    public void SetVariable(string varName, object value)
    {
        _environment.SetVariable(varName, value);
        CalculateSheet();
    }
}