using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Graph;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Dependencies;

public class DependencyManager
{
    private readonly DependencyGraph<FormulaVertex> _dependencyGraph = new();

    /// <summary>
    /// Stores regions that are referenced by formula vertices, and holds a reference
    /// to each formula vertex that references the region.
    /// E.g. for a formula "=A1 + sum(B1:B2)", the region A1 and B1:B2 will have a reference to the formula vertex
    /// </summary>
    private readonly RegionDataStore<FormulaVertex> _referencedVertexStore = new();

    internal int FormulaCount => _dependencyGraph.Count;

    public DependencyManagerRestoreData SetFormula(int row, int col, CellFormula? formula)
    {
        var formulaVertex = new FormulaVertex(row, col, formula);
        // Clear any dependency tracking for old formula if there is one
        var restoreData = ClearFormula(row, col);

        if (formula == null)
            return restoreData;

        _dependencyGraph.AddVertex(formulaVertex);
        restoreData.VerticesAdded.Add(formulaVertex);

        foreach (var formulaRef in formula.References)
        {
            // add edges to any formula that already exist
            if (formulaRef is not NamedReference)
            {
                var formulaReferringToRegion = _referencedVertexStore.GetData(formulaRef.Region);
                foreach (var f in formulaReferringToRegion)
                {
                    _dependencyGraph.AddEdge(f, formulaVertex);
                    restoreData.EdgesAdded.Add((f, formulaVertex));
                }

                restoreData.RegionRestoreData.Merge(
                    _referencedVertexStore.Add(formulaRef.Region.Clone(), formulaVertex));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return restoreData;
    }

    public DependencyManagerRestoreData ClearFormula(int row, int col)
    {
        var restoreData = new DependencyManagerRestoreData();
        var formulaVertex = new FormulaVertex(row, col, null);
        if (!_dependencyGraph.HasVertex(formulaVertex.Key))
            return restoreData;

        formulaVertex = _dependencyGraph.GetVertex(formulaVertex.Key);

        // remove the references that refer to this formula cell
        var formulaReferences = formulaVertex.Formula?.References;

        if (formulaReferences != null)
        {
            foreach (var formulaRef in formulaReferences)
            {
                List<DataRegion<FormulaVertex>> dataToDelete =  []
                ;
                switch (formulaRef)
                {
                    case CellReference cellRef:
                        dataToDelete = _referencedVertexStore
                            .GetDataRegions(new Region(cellRef.RowIndex, cellRef.ColIndex), formulaVertex).ToList();
                        break;
                    case RangeReference rangeReference:
                        dataToDelete = _referencedVertexStore
                            .GetDataRegions(rangeReference.Region, formulaVertex).ToList();
                        break;
                    case NamedReference namedReference:
                        throw new NotImplementedException();
                        break;
                }

                if (dataToDelete.Count != 0)
                    restoreData.RegionRestoreData = _referencedVertexStore.Delete(dataToDelete);
            }
        }

        foreach (var vertex in _dependencyGraph.Adj(formulaVertex))
            restoreData.EdgesRemoved.Add((formulaVertex, vertex));

        foreach (var vertex in _dependencyGraph.Prec(formulaVertex))
            restoreData.EdgesRemoved.Add((vertex, formulaVertex));

        _dependencyGraph.RemoveVertex(formulaVertex);

        restoreData.VerticesRemoved.Add(formulaVertex);
        return restoreData;
    }

    public bool HasDependents(IRegion region)
    {
        return _referencedVertexStore.Any(region);
    }

    public bool HasDependents(int row, int col)
    {
        return _referencedVertexStore.Any(row, col) ||
               _dependencyGraph.HasVertex((new FormulaVertex(row, col, null)).Key);
    }

    /// <summary>
    /// Returns the vertices that are directly dependent on the given region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<FormulaVertex> GetDirectDependents(IRegion region)
    {
        return _referencedVertexStore.GetData(region);
    }

    public DependencyManagerRestoreData InsertRowAt(int row, int count) =>
        InsertRowColAt(row, count, Axis.Row);

    public DependencyManagerRestoreData InsertRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new DependencyManagerRestoreData()
        {
            Shifts = { new AppliedShift(axis, index, count) }
        };
        IRegion affectedRegion = axis == Axis.Col
            ? new ColumnRegion(index, int.MaxValue)
            : new RowRegion(index, int.MaxValue);

        int dCol = axis == Axis.Col ? count : 0;
        int dRow = axis == Axis.Row ? count : 0;

        // find anything that depends directly on the regions that are shifted
        // and shift the formula references
        // needs to be done before we shift vertices

        var formulaDependents = GetDirectDependents(affectedRegion);

        foreach (var dependent in formulaDependents)
        {
            // capture the current references before they are modified
            var existing = dependent.Formula!.References.Select(r => r.Region.Clone()).ToList();
            restoreData.ModifiedReferenceRegions.Add((dependent.Formula, existing));
            dependent.Formula!.InsertRowColIntoReferences(index, count, axis);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol));
        restoreData.RegionRestoreData = _referencedVertexStore.InsertRowColAt(index, count, axis);

        return restoreData;
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

    public DependencyManagerRestoreData InsertColAt(int col, int count) =>
        InsertRowColAt(col, count, Axis.Col);

    public DependencyManagerRestoreData RemoveColAt(int col, int count) =>
        RemoveRowColAt(col, count, Axis.Col);

    public DependencyManagerRestoreData RemoveRowAt(int row, int count) =>
        RemoveRowColAt(row, count, Axis.Row);

    public DependencyManagerRestoreData RemoveRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new DependencyManagerRestoreData()
        {
            Shifts = { new AppliedShift(axis, index, -count) }
        };
        IRegion regionRemoved =
            axis == Axis.Col
                ? new ColumnRegion(index, index + count - 1)
                : new RowRegion(index, index + count - 1);

        // remove any formula in the region being removed
        var vertices = GetVerticesInRegion(regionRemoved);
        foreach (var vertex in vertices)
        {
            restoreData.Merge(ClearFormula(vertex.Region!.Top, vertex.Region!.Left));
        }

        int dCol = axis == Axis.Col ? -count : 0;
        int dRow = axis == Axis.Row ? -count : 0;

        // find anything that depends directly on the regions that are shifted
        // and modify the formula references
        // needs to be done before we shift vertices

        IRegion affectedRegion = axis == Axis.Col
            ? new ColumnRegion(index, int.MaxValue)
            : new RowRegion(index, int.MaxValue);

        var dependentFormula =
            _referencedVertexStore.GetData(affectedRegion);

        foreach (var dependent in dependentFormula)
        {
            // capture the current references before they are modified
            var existing = dependent.Formula!.References.Select(r => r.Region.Clone()).ToList();
            restoreData.ModifiedReferenceRegions.Add((dependent.Formula, existing));
            dependent.Formula!.RemoveRowColFromReferences(index, count, axis);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol));
        restoreData.RegionRestoreData = _referencedVertexStore.RemoveRowColAt(index, index + count - 1, axis);
        return restoreData;
    }

    private DependencyManagerRestoreData ShiftVerticesInRegion(IRegion region, int dRow, int dCol)
    {
        var restoreData = new DependencyManagerRestoreData();
        // shift any affected vertices by the number inserted
        var affectedVertices = GetVerticesInRegion(region);
        foreach (var v in affectedVertices)
        {
            var shiftedR = v.Region!.Clone();
            shiftedR.Shift(dRow, dCol);

            var shiftedV = new FormulaVertex(shiftedR, v.Formula);
            restoreData.VerticesAdded.Add(shiftedV);
            restoreData.VerticesRemoved.Add(v);

            foreach (var vertex in _dependencyGraph.Adj(v))
            {
                restoreData.EdgesRemoved.Add((v, vertex));
                restoreData.EdgesAdded.Add((shiftedV, vertex));
            }

            foreach (var vertex in _dependencyGraph.Prec(v))
            {
                restoreData.EdgesRemoved.Add((vertex, v));
                restoreData.EdgesAdded.Add((shiftedV, v));
            }

            _dependencyGraph.Swap(v, shiftedV);
        }

        return restoreData;
    }

    public IEnumerable<FormulaVertex> GetCalculationOrder()
    {
        var sort = new TopologicalSort<FormulaVertex>();
        return sort.Sort(_dependencyGraph);
    }

    public void Restore(DependencyManagerRestoreData restoreData)
    {
        foreach (var vertex in restoreData.VerticesAdded)
        {
            _dependencyGraph.RemoveVertex(vertex);
        }

        foreach (var vertex in restoreData.VerticesRemoved)
        {
            _dependencyGraph.AddVertex(vertex);
        }

        foreach (var edge in restoreData.EdgesAdded)
        {
            _dependencyGraph.RemoveEdge(edge.Item1, edge.Item2);
        }

        foreach (var edge in restoreData.EdgesRemoved)
        {
            _dependencyGraph.AddEdge(edge.Item1, edge.Item2);
        }

        // 1. shift & restore referenced vertex store
        _referencedVertexStore.Restore(restoreData.RegionRestoreData);

        // 2. restore contrracted/expanded/shifted formula references from the records

        foreach (var regionModification in restoreData.ModifiedReferenceRegions)
        {
            int refIndex = 0;
            foreach (var formulaReference in regionModification.formula.References)
            {
                formulaReference.SetRegion(regionModification.oldReferences[refIndex++]);
            }
        }
    }
}

public class DependencyManagerRestoreData
{
    public RegionRestoreData<FormulaVertex> RegionRestoreData { get; set; } = new();
    public List<FormulaVertex> VerticesRemoved { get; set; } = new();
    public List<FormulaVertex> VerticesAdded { get; set; } = new();
    public readonly List<(FormulaVertex, FormulaVertex)> EdgesRemoved = new();
    public readonly List<(FormulaVertex, FormulaVertex)> EdgesAdded = new();
    public readonly List<AppliedShift> Shifts = new();
    public readonly List<(CellFormula formula, List<IRegion> oldReferences)> ModifiedReferenceRegions = new();

    public void Merge(DependencyManagerRestoreData other)
    {
        RegionRestoreData.Merge(other.RegionRestoreData);
        VerticesAdded.AddRange(other.VerticesAdded);
        VerticesRemoved.AddRange(other.VerticesRemoved);
        EdgesAdded.AddRange(other.EdgesAdded);
        EdgesRemoved.AddRange(other.EdgesRemoved);
        Shifts.AddRange(other.Shifts);
        ModifiedReferenceRegions.AddRange(other.ModifiedReferenceRegions);
    }
}