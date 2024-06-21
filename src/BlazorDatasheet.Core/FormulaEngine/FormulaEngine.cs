using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.DataStructures.Util;
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
    private readonly SheetEnvironment _environment;
    private readonly Parser _parser = new();
    private readonly Evaluator _evaluator;
    private readonly DependencyGraph<CellFormula?> _dependencyGraph;

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
        _dependencyGraph = new DependencyGraph<CellFormula?>();
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
        return _dependencyGraph.HasVertex(RangeText.ToCellText(row, col)) ||
               _observedRanges.Any(row, col);
    }

    private bool RegionContainsReferencedCells(IRegion region)
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
        var cellVertex = new RegionVertex(row, col, formula);
        _dependencyGraph.AddVertex(cellVertex);

        var references = formula.References;
        foreach (var reference in references)
        {
            var referenceVertex = GetVertexFromReference(reference);
            _dependencyGraph.AddEdge(referenceVertex, cellVertex);
            if (reference is RangeReference rangeReference)
            {
                _observedRanges.Add(((RegionVertex)referenceVertex).Region, true);
                var cellVerticesInsideRange = GetSingleCellVerticesInsideRange(rangeReference);
                // We depend on all cells inside the range, so ensure that they are calculated first
                // by adding the correct dependency - the region depends on everything inside
                foreach (var vertex in cellVerticesInsideRange)
                {
                    if (vertex != cellVertex)
                        _dependencyGraph.AddEdge(vertex, referenceVertex);
                }
            }
        }
    }

    private IEnumerable<RegionVertex> GetSingleCellVerticesInsideRange(RangeReference rangeRef)
    {
        var region = ToRegion(rangeRef);
        if (region == null)
            return Enumerable.Empty<RegionVertex>();

        var cellVertices = new List<RegionVertex>();

        foreach (var vertex in _dependencyGraph.GetAll())
        {
            if (vertex is RegionVertex rangeVertex &&
                rangeVertex.Region.Width == 1 && rangeVertex.Region.Height == 1 &&
                region.Contains(rangeVertex.Region.Top, rangeVertex.Region.Left))
            {
                cellVertices.Add(rangeVertex);
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
            return CellValue.Error(ErrorType.Na, $"Error running formula: {e.Message}");
        }
    }

    /// <summary>
    /// Removes any vertices that the formula in this cell is dependent on
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void RemoveFormula(int row, int col)
    {
        var vertex = new RegionVertex(row, col, null);
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
            if (vertex is RegionVertex cellVertex && cellVertex.Region.IsSingleCell())
            {
                var formula = _sheet.Cells.GetFormula(cellVertex.Region.Top, cellVertex.Region.Left);
                if (formula != null)
                {
                    var value = this.Evaluate(formula);
                    _sheet.Cells.SetValueImpl(cellVertex.Region.Top, cellVertex.Region.Left, value);
                    _sheet.MarkDirty(cellVertex.Region.Top, cellVertex.Region.Left);
                }
            }
        }

        _sheet.EndBatchUpdates();

        IsCalculating = false;
    }

    private Vertex<CellFormula?> GetVertexFromReference(Reference reference)
    {
        if (reference is CellReference cellReference)
        {
            var formula = _sheet.Cells.GetFormula(cellReference.Row.RowNumber, cellReference.Col.ColNumber);
            return new RegionVertex(cellReference.Row.RowNumber, cellReference.Col.ColNumber, formula);
        }

        if (reference is NamedReference namedReference)
        {
            return new NamedVertex(namedReference.Name, null);
        }

        if (reference is RangeReference rangeReference)
        {
            var region = ToRegion(rangeReference);
            if (region != null)
            {
                return new RegionVertex(region, null);
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

    public void InsertRowColAt(int index, int count, Axis axis)
    {
        foreach (var vertex in _dependencyGraph.GetAll())
        {
            if (vertex.Data != null)
            {
                foreach (var reference in vertex.Data.References)
                {
                    
                }
            }
        }
    }

    public IEnumerable<Vertex<CellFormula?>> GetVerticesInRegion(IRegion region)
    {
        var vertices = new List<RegionVertex>();
        foreach (var vertex in _dependencyGraph.GetAll())
        {
            if (vertex is RegionVertex regionVertex && region.Contains(regionVertex.Region))
            {
                vertices.Add(regionVertex);
            }
        }

        return vertices;
    }
}