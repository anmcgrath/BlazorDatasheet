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
    private readonly Dictionary<string, RegionDataStore<FormulaVertex>> _referencedVertexStores = new();

    private readonly HashSet<FormulaVertex> _volatileVertices = new();

    internal int FormulaCount => _dependencyGraph.Count;

    private RegionDataStore<FormulaVertex> GetReferencedVertexStore(string sheetName)
    {
        return _referencedVertexStores.GetValueOrDefault(sheetName) ?? new RegionDataStore<FormulaVertex>();
    }

    public void AddSheet(string sheetName)
    {
        _referencedVertexStores.Add(sheetName, new RegionDataStore<FormulaVertex>());
    }

    public void RemoveSheet(string sheetName)
    {
        _referencedVertexStores.Remove(sheetName);
    }

    public void RenameSheet(string oldName, string newName)
    {
        foreach (var vertex in _dependencyGraph.GetAll().ToList())
        {
            var formula = vertex.Formula;
            if (formula == null)
                continue;

            if (vertex.SheetName == oldName)
            {
                _dependencyGraph.Swap(vertex,
                    new FormulaVertex(vertex.Row, vertex.Col, newName, vertex.Formula));
            }

            foreach (var formulaRef in formula.References)
            {
                if (formulaRef.SheetName == oldName)
                    formulaRef.SetSheetName(newName, formulaRef.ExplicitSheetName);
            }
        }

        var existingRefStore = GetReferencedVertexStore(oldName);
        _referencedVertexStores.Add(newName, existingRefStore);
        _referencedVertexStores.Remove(oldName);
    }

    public DependencyManagerRestoreData SetFormula(int row, int col, string sheetName, CellFormula? formula)
    {
        var formulaVertex = new FormulaVertex(row, col, sheetName, formula);
        return SetFormulaVertex(formulaVertex);
    }

    public DependencyManagerRestoreData SetFormula(string name, CellFormula? formula)
    {
        var formulaVertex = new FormulaVertex(name, formula);
        return SetFormulaVertex(formulaVertex);
    }

    public CellFormula? GetFormula(string name)
    {
        var vertex = GetVertex(name);
        return vertex?.Formula;
    }

    private DependencyManagerRestoreData SetFormulaVertex(FormulaVertex formulaVertex)
    {
        // Clear any dependency tracking for old formula if there is one
        var restoreData = ClearFormula(formulaVertex);

        if (formulaVertex.Formula == null)
            return restoreData;

        _dependencyGraph.AddVertex(formulaVertex);
        restoreData.VerticesAdded.Add(formulaVertex);

        if (formulaVertex.Formula.ContainsVolatiles)
            _volatileVertices.Add(formulaVertex);

        AddIncomingEdgesForFormulaReferences(formulaVertex, restoreData);
        AddOutgoingEdgesToDirectDependents(formulaVertex, restoreData);

        return restoreData;
    }

    private DependencyManagerRestoreData ClearFormula(FormulaVertex formulaVertex)
    {
        var restoreData = new DependencyManagerRestoreData();
        _volatileVertices.Remove(formulaVertex);

        if (!_dependencyGraph.HasVertex(formulaVertex.Key))
            return restoreData;

        var existingFormulaVertex = _dependencyGraph.GetVertex(formulaVertex.Key);
        if (existingFormulaVertex == null)
            return restoreData;
        formulaVertex = existingFormulaVertex;

        RemoveReferenceTrackingForFormula(formulaVertex, restoreData);
        RecordRemovedEdges(formulaVertex, restoreData);

        _dependencyGraph.RemoveVertex(formulaVertex, false);
        restoreData.VerticesRemoved.Add(formulaVertex);

        return restoreData;
    }

    public DependencyManagerRestoreData ClearFormula(int row, int col, string sheetName)
    {
        var formulaVertex = new FormulaVertex(row, col, sheetName, null);
        return ClearFormula(formulaVertex);
    }

    public DependencyManagerRestoreData ClearFormula(string name)
    {
        var formulaVertex = new FormulaVertex(name, null);
        return ClearFormula(formulaVertex);
    }

    public bool HasDependents(IRegion region, string sheetName)
    {
        return GetReferencedVertexStore(sheetName).Any(region);
    }

    public bool HasDependents(int row, int col, string sheetName)
    {
        var formulaReferenced = _dependencyGraph.HasVertex(FormulaVertex.GetKey(row, col, sheetName));
        if (formulaReferenced)
            return true;
        return GetReferencedVertexStore(sheetName).Any(row, col);
    }

    /// <summary>
    /// Returns the vertices that are directly dependent on the given region
    /// </summary>
    /// <param name="region"></param>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    public IEnumerable<FormulaVertex> GetDirectDependents(IRegion region, string sheetName)
    {
        return GetReferencedVertexStore(sheetName).GetData(region);
    }

    public IEnumerable<FormulaVertex> GetDirectDependents(FormulaVertex vertex)
    {
        if (vertex.VertexType == VertexType.Cell)
        {
            return GetDirectDependents(new Region(vertex.Row, vertex.Col), vertex.SheetName);
        }

        return _dependencyGraph.GetAll()
            .Where(x => x.Formula?.References.Any(r => r is NamedReference n && n.Name == vertex.Key) == true);
    }

    public DependencyManagerRestoreData InsertRowAt(int row, int count, string sheetName) =>
        InsertRowColAt(row, count, Axis.Row, sheetName);

    public DependencyManagerRestoreData InsertRowColAt(int index, int count, Axis axis, string sheetName)
    {
        var restoreData = new DependencyManagerRestoreData()
        {
            Shifts = { new AppliedShift(axis, index, count, sheetName) }
        };
        var affectedRegion = GetAffectedRegion(axis, index);
        var (dRow, dCol) = GetShiftDelta(axis, count);

        // find anything that depends directly on the regions that are shifted
        // and shift the formula references
        // needs to be done before we shift vertices


        var formulaDependents = GetDirectDependents(affectedRegion, sheetName);

        foreach (var dependent in formulaDependents)
        {
            var formula = dependent.Formula;
            if (formula == null)
                continue;

            restoreData.ModifiedFormulaReferences.Add(CaptureReferenceRestoreData(formula));
            formula.InsertRowColIntoReferences(index, count, axis, sheetName);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol, sheetName));
        restoreData.MergeRegionRestoreData(
            sheetName,
            GetReferencedVertexStore(sheetName).InsertRowColAt(index, count, axis));

        return restoreData;
    }

    private IEnumerable<FormulaVertex> GetVerticesInRegion(IRegion region, string sheetName)
    {
        if (region.IsSingleCell())
        {
            var vertex = _dependencyGraph.GetVertex(FormulaVertex.GetKey(region.Top, region.Left, sheetName));
            if (vertex != null)
                return [vertex];
            return Array.Empty<FormulaVertex>();
        }

        var vertices = new List<FormulaVertex>();
        foreach (var v in _dependencyGraph.GetAll())
        {
            if (v.Position == null)
                continue;

            if (v.SheetName == sheetName && region.Contains(v.Row, v.Col))
            {
                vertices.Add(v);
            }
        }

        return vertices;
    }

    public DependencyManagerRestoreData InsertColAt(int col, int count, string sheetName) =>
        InsertRowColAt(col, count, Axis.Col, sheetName);

    public DependencyManagerRestoreData RemoveColAt(int col, int count, string sheetName) =>
        RemoveRowColAt(col, count, Axis.Col, sheetName);

    public DependencyManagerRestoreData RemoveRowAt(int row, int count, string sheetName) =>
        RemoveRowColAt(row, count, Axis.Row, sheetName);

    public DependencyManagerRestoreData RemoveRowColAt(int index, int count, Axis axis, string sheetName)
    {
        var restoreData = new DependencyManagerRestoreData()
        {
            Shifts = { new AppliedShift(axis, index, -count, sheetName) }
        };
        var regionRemoved = GetRemovedRegion(axis, index, count);

        // remove any formula in the region being removed
        var vertices = GetVerticesInRegion(regionRemoved, sheetName);
        foreach (var vertex in vertices)
        {
            if (vertex.Position is { } position)
                restoreData.Merge(ClearFormula(position.row, position.col, sheetName));
        }

        var (dRow, dCol) = GetShiftDelta(axis, -count);

        // find anything that depends directly on the regions that are shifted
        // and modify the formula references
        // needs to be done before we shift vertices
        var affectedRegion = GetAffectedRegion(axis, index);

        var dependentFormulas = GetDirectDependents(affectedRegion, sheetName);

        foreach (var dependent in dependentFormulas)
        {
            var formula = dependent.Formula;
            if (formula == null)
                continue;

            restoreData.ModifiedFormulaReferences.Add(CaptureReferenceRestoreData(formula));
            formula.RemoveRowColFromReferences(index, count, axis, sheetName);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol, sheetName));
        restoreData.MergeRegionRestoreData(
            sheetName,
            GetReferencedVertexStore(sheetName).RemoveRowColAt(index, count, axis));

        return restoreData;
    }

    private DependencyManagerRestoreData ShiftVerticesInRegion(IRegion region, int dRow, int dCol, string sheetName)
    {
        var restoreData = new DependencyManagerRestoreData();
        // shift any affected vertices by the number inserted
        var affectedVertices = GetVerticesInRegion(region, sheetName);
        foreach (var v in affectedVertices)
        {
            // need to shift without changing the reference
            // needs to update key in dependency graph
            // and also shift the region it refers to
            if (v.Position != null)
                v.Position = new CellPosition(v.Row + dRow, v.Col + dCol);
            _dependencyGraph.RefreshKey(v);
        }

        return restoreData;
    }

    /// <summary>
    /// Returns the topological sort of the vertices. Each group of vertices is a strongly connected group.
    /// </summary>
    /// <returns></returns>
    public IList<IList<FormulaVertex>> GetCalculationOrder(IEnumerable<FormulaVertex>? dirtyFormula = null)
    {
        var sort = new SccSort<FormulaVertex>(_dependencyGraph);
        if (dirtyFormula == null && _volatileVertices.Count == 0)
            return sort.Sort();

        return sort.Sort((dirtyFormula ?? []).Concat(_volatileVertices));
    }

    public IEnumerable<DependencyInfo> GetDependencies()
    {
        var results = new List<DependencyInfo>();
        foreach (var vertex in _dependencyGraph.GetAll())
        {
            foreach (var dependent in _dependencyGraph.GetDependentsOf(vertex))
            {
                if (dependent.VertexType != VertexType.Named)
                {
                    results.Add(new DependencyInfo(new Region(dependent.Row, dependent.Col),
                        new Region(vertex.Row, vertex.Col),
                        DependencyType.CalculationOrder));
                }
            }
        }

        var dataRegions = _referencedVertexStores.SelectMany(x => x.Value.GetAllDataRegions());
        foreach (var region in dataRegions)
        {
            results.Add(new DependencyInfo(new Region(region.Data.Row, region.Data.Col), region.Region,
                DependencyType.Region));
        }

        return results;
    }

    public void Restore(DependencyManagerRestoreData restoreData)
    {
        RestoreShiftedVertices(restoreData);
        RestoreVertices(restoreData);
        RestoreEdges(restoreData);
        RestoreReferencedVertexStores(restoreData);
        RestoreModifiedFormulaReferences(restoreData);
    }

    private void AddIncomingEdgesForFormulaReferences(FormulaVertex formulaVertex, DependencyManagerRestoreData restoreData)
    {
        foreach (var formulaRef in formulaVertex.Formula!.References)
        {
            if (formulaRef is NamedReference namedRef)
            {
                if (_dependencyGraph.GetVertex(namedRef.Name) is { } existingVertex)
                    _dependencyGraph.AddEdge(existingVertex, formulaVertex);
                continue;
            }

            foreach (var precedent in GetVerticesInRegion(formulaRef.Region, formulaRef.SheetName))
            {
                _dependencyGraph.AddEdge(precedent, formulaVertex);
                restoreData.EdgesAdded.Add((precedent.Key, formulaVertex.Key));
            }

            restoreData.MergeRegionRestoreData(
                formulaRef.SheetName,
                GetReferencedVertexStore(formulaRef.SheetName).Add(formulaRef.Region.Clone(), formulaVertex));
        }
    }

    private void AddOutgoingEdgesToDirectDependents(FormulaVertex formulaVertex, DependencyManagerRestoreData restoreData)
    {
        foreach (var dependent in GetDirectDependents(formulaVertex))
        {
            _dependencyGraph.AddEdge(formulaVertex, dependent);
            restoreData.EdgesAdded.Add((formulaVertex.Key, dependent.Key));
        }
    }

    private void RemoveReferenceTrackingForFormula(FormulaVertex formulaVertex, DependencyManagerRestoreData restoreData)
    {
        var formulaReferences = formulaVertex.Formula?.References;
        if (formulaReferences == null)
            return;

        foreach (var formulaRef in formulaReferences)
        {
            var dataToDelete = GetTrackedRegionsForReference(formulaRef, formulaVertex);
            if (dataToDelete.Count == 0)
                continue;

            restoreData.MergeRegionRestoreData(
                formulaRef.SheetName,
                GetReferencedVertexStore(formulaRef.SheetName).Delete(dataToDelete));
        }
    }

    private List<DataRegion<FormulaVertex>> GetTrackedRegionsForReference(Reference formulaRef, FormulaVertex formulaVertex)
    {
        return formulaRef switch
        {
            CellReference cellRef => GetReferencedVertexStore(cellRef.SheetName)
                .GetDataRegions(cellRef.RowIndex, cellRef.ColIndex, formulaVertex)
                .ToList(),
            RangeReference rangeRef => GetReferencedVertexStore(rangeRef.SheetName)
                .GetDataRegions(rangeRef.Region, formulaVertex)
                .ToList(),
            _ => []
        };
    }

    private void RecordRemovedEdges(FormulaVertex formulaVertex, DependencyManagerRestoreData restoreData)
    {
        foreach (var dependent in _dependencyGraph.GetDependentsOf(formulaVertex))
            restoreData.EdgesRemoved.Add((formulaVertex.Key, dependent.Key));

        foreach (var precedent in _dependencyGraph.GetPrecedentsOf(formulaVertex))
            restoreData.EdgesRemoved.Add((precedent.Key, formulaVertex.Key));
    }

    private void RestoreShiftedVertices(DependencyManagerRestoreData restoreData)
    {
        foreach (var shift in restoreData.Shifts)
        {
            if (shift.SheetName == null)
                continue;

            var affectedRegion = GetAffectedRegion(shift.Axis, shift.Index);
            var (dRow, dCol) = GetShiftDelta(shift.Axis, -shift.Amount);
            ShiftVerticesInRegion(affectedRegion, dRow, dCol, shift.SheetName);
        }
    }

    private void RestoreVertices(DependencyManagerRestoreData restoreData)
    {
        foreach (var vertex in restoreData.VerticesAdded)
            _dependencyGraph.RemoveVertex(vertex);

        foreach (var vertex in restoreData.VerticesRemoved)
            _dependencyGraph.AddVertex(vertex);
    }

    private void RestoreEdges(DependencyManagerRestoreData restoreData)
    {
        foreach (var edge in restoreData.EdgesAdded)
            _dependencyGraph.RemoveEdge(edge.Item1, edge.Item2);

        foreach (var edge in restoreData.EdgesRemoved)
            _dependencyGraph.AddEdge(edge.Item1, edge.Item2);
    }

    private void RestoreReferencedVertexStores(DependencyManagerRestoreData restoreData)
    {
        foreach (var (sheetName, sheetRestoreData) in restoreData.RegionRestoreDataBySheet)
        {
            if (_referencedVertexStores.TryGetValue(sheetName, out var store))
                store.Restore(sheetRestoreData);
        }
    }

    private static void RestoreModifiedFormulaReferences(DependencyManagerRestoreData restoreData)
    {
        foreach (var regionModification in restoreData.ModifiedFormulaReferences)
            RestoreFormulaReferences(regionModification);
    }

    private static ReferenceRestoreData CaptureReferenceRestoreData(CellFormula formula)
    {
        var existingRegions = formula.References.Select(r => r.Region.Clone()).ToList();
        var existingValidities = formula.References.Select(r => r.IsInvalid).ToList();
        var sheetNames = formula.References.Select(x => x.SheetName).ToList();
        var explicitSheetNames = formula.References.Select(x => x.ExplicitSheetName).ToList();
        return new ReferenceRestoreData(formula, existingRegions, existingValidities, sheetNames, explicitSheetNames);
    }

    private static void RestoreFormulaReferences(ReferenceRestoreData regionModification)
    {
        int refIndex = 0;
        foreach (var formulaReference in regionModification.Formula.References)
        {
            formulaReference.SetSheetName(regionModification.SheetNames[refIndex],
                regionModification.ExplicitSheetReferences[refIndex]);
            formulaReference.SetRegion(regionModification.OldRegions[refIndex]);
            formulaReference.SetValidity(!regionModification.OldInvalidStates[refIndex]);
            refIndex++;
        }
    }

    private static IRegion GetAffectedRegion(Axis axis, int index)
    {
        return axis == Axis.Col
            ? new ColumnRegion(index, int.MaxValue)
            : new RowRegion(index, int.MaxValue);
    }

    private static IRegion GetRemovedRegion(Axis axis, int index, int count)
    {
        return axis == Axis.Col
            ? new ColumnRegion(index, index + count - 1)
            : new RowRegion(index, index + count - 1);
    }

    private static (int dRow, int dCol) GetShiftDelta(Axis axis, int amount)
    {
        var dRow = axis == Axis.Row ? amount : 0;
        var dCol = axis == Axis.Col ? amount : 0;
        return (dRow, dCol);
    }

    public IEnumerable<FormulaVertex> FindDependentFormula(IRegion region, string sheetName)
    {
        return GetReferencedVertexStore(sheetName).GetData(region);
    }

    public FormulaVertex? GetVertex(int cellRow, int cellCol, string sheetName)
    {
        return _dependencyGraph.GetVertex(FormulaVertex.GetKey(cellRow, cellCol, sheetName));
    }

    public FormulaVertex? GetVertex(string name)
    {
        return _dependencyGraph.GetVertex(name);
    }

    public IEnumerable<FormulaVertex> GetAllVertices() => _dependencyGraph.GetAll();
}

public class DependencyManagerRestoreData
{
    public Dictionary<string, RegionRestoreData<FormulaVertex>> RegionRestoreDataBySheet { get; } = new();
    public List<FormulaVertex> VerticesRemoved { get; set; } = new();
    public List<FormulaVertex> VerticesAdded { get; set; } = new();
    public readonly List<(string, string)> EdgesRemoved = new();
    public readonly List<(string, string)> EdgesAdded = new();
    public readonly List<AppliedShift> Shifts = new();
    internal readonly List<ReferenceRestoreData> ModifiedFormulaReferences = new();

    public void MergeRegionRestoreData(string sheetName, RegionRestoreData<FormulaVertex> restoreData)
    {
        if (RegionRestoreDataBySheet.TryGetValue(sheetName, out var existing))
            existing.Merge(restoreData);
        else
            RegionRestoreDataBySheet[sheetName] = restoreData;
    }

    public void Merge(DependencyManagerRestoreData other)
    {
        foreach (var (sheetName, sheetRestoreData) in other.RegionRestoreDataBySheet)
        {
            MergeRegionRestoreData(sheetName, sheetRestoreData);
        }
        VerticesAdded.AddRange(other.VerticesAdded);
        VerticesRemoved.AddRange(other.VerticesRemoved);
        EdgesAdded.AddRange(other.EdgesAdded);
        EdgesRemoved.AddRange(other.EdgesRemoved);
        Shifts.AddRange(other.Shifts);
        ModifiedFormulaReferences.AddRange(other.ModifiedFormulaReferences);
    }
}

internal class ReferenceRestoreData
{
    public CellFormula Formula { get; }
    public List<IRegion> OldRegions { get; }
    public List<bool> OldInvalidStates { get; }
    public List<string> SheetNames { get; }

    public List<bool> ExplicitSheetReferences { get; }

    public ReferenceRestoreData(CellFormula formula, List<IRegion> oldRegions, List<bool> oldInvalidStates,
        List<string> sheetNames, List<bool> explicitSheetReferences)
    {
        Formula = formula;
        OldRegions = oldRegions;
        OldInvalidStates = oldInvalidStates;
        SheetNames = sheetNames;
        ExplicitSheetReferences = explicitSheetReferences;
    }
}
