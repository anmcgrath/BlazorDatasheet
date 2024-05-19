using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
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
    private SheetEnvironment _environment;
    private readonly Parser _parser = new();
    private readonly Evaluator _evaluator;
    private readonly DependencyGraph _dependencyGraph;

    /// <summary>
    /// Keeps track of any ranges referenced by formula.
    /// This should ideally keep track of the formula that reference the range also,
    /// but for now it's just whether it's referenced or not.
    /// </summary>
    private readonly RegionDataStore<bool> _observedRanges;

    public bool IsCalculating { get; private set; }

    public FormulaEngine(Sheet sheet)
    {
        _sheet = sheet;
        _cells = sheet.Cells;
        _sheet.Editor.BeforeCellEdit += SheetOnBeforeCellEdit;
        _cells.CellsChanged += SheetOnCellsChanged;

        _environment = new SheetEnvironment(sheet);
        _evaluator = new Evaluator(_environment);
        _dependencyGraph = new DependencyGraph();
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
                if (IsRegionReferenced(region))
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
        return _dependencyGraph.HasVertex(new CellVertex(row, col).Key) ||
               _observedRanges.Any(row, col);
    }

    private bool IsRegionReferenced(IRegion region)
    {
        return _observedRanges.Any(region);
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

    public void AddToDependencyGraph(int row, int col, CellFormula formula)
    {
        var formulaVertex = new CellVertex(row, col);
        _dependencyGraph.AddVertex(formulaVertex);

        var references = formula.References!;
        foreach (var reference in references)
        {
            var vertex = GetVertex(reference);
            _dependencyGraph.AddEdge(vertex, formulaVertex);
            if (reference is RangeReference rangeReference)
            {
                _observedRanges.Add(((RegionVertex)vertex).Region, true);
                var cellVerticesInsideRange = GetCellVerticesInRange(rangeReference);
                foreach (var cellVertex in cellVerticesInsideRange)
                {
                    if (cellVertex != formulaVertex)
                        _dependencyGraph.AddEdge(cellVertex, vertex);
                }
            }
        }
    }

    private IEnumerable<CellVertex> GetCellVerticesInRange(RangeReference rangeRef)
    {
        var region = ToRegion(rangeRef);
        if (region == null)
            return Enumerable.Empty<CellVertex>();

        var cellVertices = new List<CellVertex>();

        foreach (var vertex in _dependencyGraph.GetAll())
        {
            if (vertex is CellVertex cellVertex &&
                region.Contains(cellVertex.Row, cellVertex.Col))
            {
                cellVertices.Add(cellVertex);
            }
        }

        return cellVertices;
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
            return _evaluator.Evaluate(formula, resolveReferences);
        }
        catch (Exception e)
        {
            return CellValue.Error(ErrorType.Na, "Error running formula");
        }
    }

    /// <summary>
    /// Removes any vertices that the formula in this cell is dependent on
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void RemoveFormula(int row, int col)
    {
        var vertex = new CellVertex(row, col);
        var dependsOn = _dependencyGraph.Prec(vertex);
        foreach (var dependent in dependsOn)
        {
            _dependencyGraph.RemoveEdge(dependent, vertex);
        }
    }

    public void CalculateSheet()
    {
        if (IsCalculating)
            return;

        IsCalculating = true;
        _sheet.BatchUpdates();

        var order =
            _dependencyGraph
                .TopologicalSort();

        foreach (var vertex in order)
        {
            if (vertex is CellVertex cellVertex)
            {
                var formula = _sheet.Cells.GetFormula(cellVertex.Row, cellVertex.Col);
                if (formula != null)
                {
                    var value = this.Evaluate(formula);
                    _sheet.Cells.SetValueImpl(cellVertex.Row, cellVertex.Col, value);
                    _sheet.MarkDirty(cellVertex.Row, cellVertex.Col);
                }
            }
        }

        _sheet.EndBatchUpdates();

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

        if (reference is RangeReference rangeReference)
        {
            var region = ToRegion(rangeReference);
            if (region != null)
            {
                return new RegionVertex(region, rangeReference.ToRefText());
            }
        }

        throw new Exception("Could not convert reference to vertex");
    }

    private IRegion? ToRegion(RangeReference reference)
    {
        if (reference.Start is CellReference startCell && reference.End is CellReference endCell)
        {
            return new Region(startCell.Row.RowNumber, endCell.Row.RowNumber, startCell.Col.ColNumber,
                endCell.Col.ColNumber);
        }

        if (reference.Start is ColReference startCol && reference.End is ColReference endCol)
        {
            return new ColumnRegion(startCol.ColNumber, endCol.ColNumber);
        }

        if (reference.Start is RowReference startRow && reference.End is RowReference endRow)
        {
            return new RowRegion(startRow.RowNumber, endRow.RowNumber);
        }

        return null;
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