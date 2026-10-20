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

    /// <summary>
    /// Stores the location of formula vertices
    /// </summary>
    private readonly Dictionary<string, RegionDataStore<FormulaVertex>> _formulaLocations = new();

    private readonly HashSet<FormulaVertex> _volatileVertices = new();

    /// <summary>
    /// Reused across recalculations - a fresh sorter allocates its whole working set each time.
    /// </summary>
    private readonly SccSort<FormulaVertex> _sccSort;

    /// <summary>
    /// Resolves structured references (e.g. Sales[Amount]) to the regions they track. Without one,
    /// structured references stay unresolved and track nothing.
    /// </summary>
    private IStructuredReferenceResolver? _resolver;

    /// <summary>
    /// The formula vertices with structured references, by table name. A reference is indexed under the table
    /// it names, or for an unqualified reference (e.g. [@Col]) the table it resolved against; unqualified
    /// references that did not resolve to a table are indexed under "" so that a refresh retries them.
    /// </summary>
    /// <remarks>
    /// Vertices are compared by reference - a vertex's key (and hash) changes when it is shifted.
    /// </remarks>
    private readonly Dictionary<string, HashSet<FormulaVertex>> _tableDependents =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The keys of <see cref="_tableDependents"/> that each vertex is indexed under, so a vertex can be
    /// removed from the index whatever state its references are in.
    /// </summary>
    private readonly Dictionary<FormulaVertex, List<string>> _tableDependentKeys =
        new(ReferenceEqualityComparer.Instance);

    internal int FormulaCount => _dependencyGraph.Count;

    public DependencyManager()
    {
        _sccSort = new SccSort<FormulaVertex>(_dependencyGraph);
    }

    /// <summary>
    /// Sets the resolver used to find the regions that structured references track. Existing formulas are
    /// not re-resolved; call <see cref="RefreshStructuredReferences(string?)"/> for that.
    /// </summary>
    public void SetStructuredReferenceResolver(IStructuredReferenceResolver? resolver)
    {
        _resolver = resolver;
    }

    /// <summary>
    /// The store of regions referenced by formulas on <paramref name="sheetName"/>, registering the
    /// sheet if it is not yet known.
    /// </summary>
    /// <remarks>
    /// Callers write to the store they get back. Handing out a fresh, unregistered instance for an
    /// unknown sheet - as this used to - allocated on every call and silently dropped the tracking
    /// data, so a formula referencing a sheet that did not exist yet never recalculated once it did.
    /// </remarks>
    private RegionDataStore<FormulaVertex> GetReferencedVertexStore(string sheetName)
    {
        if (!_referencedVertexStores.TryGetValue(sheetName, out var store))
        {
            store = new RegionDataStore<FormulaVertex>();
            _referencedVertexStores[sheetName] = store;
        }

        return store;
    }

    private RegionDataStore<FormulaVertex> GetFormulaLocationStore(string sheetName)
    {
        if (!_formulaLocations.TryGetValue(sheetName, out var store))
        {
            store = new RegionDataStore<FormulaVertex>(expandWhenInsertAfter: false);
            _formulaLocations[sheetName] = store;
        }

        return store;
    }

    public void AddSheet(string sheetName)
    {
        // registering through the accessors keeps anything already tracked against this name - a
        // formula may have referenced the sheet before it was created.
        GetReferencedVertexStore(sheetName);
        GetFormulaLocationStore(sheetName);
    }

    public void RemoveSheet(string sheetName)
    {
        var verticesOnSheet = _dependencyGraph.GetAll()
            .Where(x => x.VertexType == VertexType.Cell && x.SheetName == sheetName)
            .Select(x => x.Position)
            .Where(x => x != null)
            .ToList();

        foreach (var position in verticesOnSheet)
            ClearFormula(position!.Value.row, position.Value.col, sheetName);

        _referencedVertexStores.Remove(sheetName);
        _formulaLocations.Remove(sheetName);
    }

    public void RenameSheet(string oldName, string newName)
    {
        var renamedVertices = new Dictionary<FormulaVertex, FormulaVertex>(ReferenceEqualityComparer.Instance);
        foreach (var vertex in _dependencyGraph.GetAll().ToList())
        {
            var formula = vertex.Formula;
            if (formula == null)
                continue;

            if (vertex.SheetName == oldName)
            {
                var renamedVertex = new FormulaVertex(vertex.Row, vertex.Col, newName, vertex.Formula);
                _dependencyGraph.Swap(vertex, renamedVertex);
                renamedVertices[vertex] = renamedVertex;

                if (_volatileVertices.Remove(vertex))
                    _volatileVertices.Add(renamedVertex);
                if (UnindexVertex(vertex))
                    IndexVertex(renamedVertex);
            }

            foreach (var formulaRef in formula.References)
            {
                if (formulaRef.SheetName == oldName)
                    formulaRef.SetSheetName(newName, formulaRef.ExplicitSheetName);
            }
        }

        var existingRefStore = GetReferencedVertexStore(oldName);
        _referencedVertexStores.Remove(oldName);
        _referencedVertexStores[newName] = existingRefStore;

        // the regions the renamed vertices' formulas read are tracked against the vertices themselves, which
        // have been replaced - without this a change on any sheet would find the old vertices, no longer in the graph
        if (renamedVertices.Count > 0)
        {
            foreach (var store in _referencedVertexStores.Values)
            {
                var stale = store.GetAllDataRegions().Where(d => renamedVertices.ContainsKey(d.Data)).ToList();
                if (stale.Count == 0)
                    continue;

                store.Delete(stale);
                foreach (var dataRegion in stale)
                    store.Add(dataRegion.Region, renamedVertices[dataRegion.Data]);
            }
        }
        
        var renamedLocationStore = new RegionDataStore<FormulaVertex>(expandWhenInsertAfter: false);
        foreach (var vertex in _dependencyGraph.GetAll())
        {
            if (vertex.VertexType == VertexType.Cell &&
                vertex.SheetName == newName)
            {
                renamedLocationStore.Add(new Region(vertex.Row, vertex.Col), vertex);
            }
        }

        _formulaLocations.Remove(oldName);
        _formulaLocations[newName] = renamedLocationStore;
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

        if (formulaVertex.VertexType == VertexType.Cell)
        {
            var store = GetFormulaLocationStore(formulaVertex.SheetName);
            restoreData.MergeFormulaLocationRestoreData(
                formulaVertex.SheetName,
                store.Add(new Region(formulaVertex.Row, formulaVertex.Col), formulaVertex));
        }

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
        // the structured references keep their resolved regions so that restoring the vertex restores them
        UnindexVertex(formulaVertex);

        _dependencyGraph.RemoveVertex(formulaVertex, false);
        restoreData.VerticesRemoved.Add(formulaVertex);
        
        if (formulaVertex.VertexType == VertexType.Cell)
        {
            var store = GetFormulaLocationStore(formulaVertex.SheetName);
            restoreData.MergeFormulaLocationRestoreData(
                formulaVertex.SheetName,
                store.Clear(new Region(formulaVertex.Row, formulaVertex.Col)));
        }

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
            .Where(x => x.Formula?.References.Any(r => r is NamedReference n && n.Name == vertex.Key.Name) == true);
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

        var tableShift = UntrackStructuredReferencesForShift(affectedRegion, axis, sheetName, restoreData);

        // find anything that depends directly on the regions that are shifted
        // and shift the formula references
        // needs to be done before we shift vertices


        var formulaDependents = GetDirectDependents(affectedRegion, sheetName).Distinct();

        foreach (var dependent in formulaDependents)
        {
            var formula = dependent.Formula;
            if (formula == null)
                continue;

            restoreData.CaptureReferences(formula);
            formula.InsertRowColIntoReferences(index, count, axis, sheetName);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol, sheetName));
        restoreData.MergeRegionRestoreData(
            sheetName,
            GetReferencedVertexStore(sheetName).InsertRowColAt(index, count, axis));

        restoreData.MergeFormulaLocationRestoreData(
            sheetName,
            GetFormulaLocationStore(sheetName).InsertRowColAt(index, count, axis));

        RetrackStructuredReferencesAfterShift(tableShift, restoreData);

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

        return GetFormulaLocationStore(sheetName).GetData(region).ToList();
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

        var tableShift = UntrackStructuredReferencesForShift(GetAffectedRegion(axis, index), axis, sheetName, restoreData);

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

        var dependentFormulas = GetDirectDependents(affectedRegion, sheetName).Distinct();

        foreach (var dependent in dependentFormulas)
        {
            var formula = dependent.Formula;
            if (formula == null)
                continue;

            restoreData.CaptureReferences(formula);
            formula.RemoveRowColFromReferences(index, count, axis, sheetName);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol, sheetName));
        restoreData.MergeRegionRestoreData(
            sheetName,
            GetReferencedVertexStore(sheetName).RemoveRowColAt(index, count, axis));

        restoreData.MergeFormulaLocationRestoreData(
            sheetName,
            GetFormulaLocationStore(sheetName).RemoveRowColAt(index, count, axis));

        RetrackStructuredReferencesAfterShift(tableShift, restoreData);

        return restoreData;
    }

    /// <summary>
    /// Inserts blank cells over <paramref name="region"/>, shifting the cells after it right (<see cref="Axis.Col"/>)
    /// or down (<see cref="Axis.Row"/>) within the band of rows (or columns) that <paramref name="region"/> covers.
    /// Only references lying entirely within the band are adjusted.
    /// </summary>
    public DependencyManagerRestoreData InsertCellsAt(IRegion region, Axis axis, string sheetName)
    {
        var count = axis == Axis.Col ? region.Width : region.Height;
        var index = region.GetLeadingEdgeOffset(axis);
        var restoreData = new DependencyManagerRestoreData()
        {
            Shifts = { new AppliedShift(axis, index, count, sheetName, region.Clone()) }
        };

        var affectedRegion = GetAffectedRegion(axis, index, region);
        var tableShift = UntrackStructuredReferencesForShift(affectedRegion, axis, sheetName, restoreData);

        AdjustDependentsForBandShift(affectedRegion, sheetName, restoreData,
            formula => formula.InsertRowColIntoReferences(index, count, axis, sheetName, region));

        ShiftVerticesInBand(affectedRegion, axis, count, sheetName, restoreData,
            () => GetFormulaLocationStore(sheetName).InsertCellsAt(region, axis, StraddlePolicy.Split));

        RetrackStructuredReferencesAfterShift(tableShift, restoreData);

        return restoreData;
    }

    /// <summary>
    /// Removes the cells in <paramref name="region"/>, shifting the cells after it left (<see cref="Axis.Col"/>)
    /// or up (<see cref="Axis.Row"/>) within the band of rows (or columns) that <paramref name="region"/> covers.
    /// Formulas inside <paramref name="region"/> are cleared. Only references lying entirely within the band are adjusted.
    /// </summary>
    public DependencyManagerRestoreData RemoveCellsAt(IRegion region, Axis axis, string sheetName)
    {
        var count = axis == Axis.Col ? region.Width : region.Height;
        var index = region.GetLeadingEdgeOffset(axis);
        var restoreData = new DependencyManagerRestoreData()
        {
            Shifts = { new AppliedShift(axis, index, -count, sheetName, region.Clone()) }
        };

        var affectedRegion = GetAffectedRegion(axis, index, region);
        var tableShift = UntrackStructuredReferencesForShift(affectedRegion, axis, sheetName, restoreData);

        // remove any formula in the region being removed
        foreach (var vertex in GetVerticesInRegion(region, sheetName).ToList())
        {
            if (vertex.Position is { } position)
                restoreData.Merge(ClearFormula(position.row, position.col, sheetName));
        }

        AdjustDependentsForBandShift(affectedRegion, sheetName, restoreData,
            formula => formula.RemoveRowColFromReferences(index, count, axis, sheetName, region));

        ShiftVerticesInBand(affectedRegion, axis, -count, sheetName, restoreData,
            () => GetFormulaLocationStore(sheetName).RemoveCellsAt(region, axis, StraddlePolicy.Split));

        RetrackStructuredReferencesAfterShift(tableShift, restoreData);

        return restoreData;
    }

    /// <summary>
    /// Adjusts the references of every formula depending on <paramref name="affectedRegion"/> and rebuilds
    /// their reference tracking from the adjusted references. The tracking is rebuilt rather than shifted so
    /// that references extending outside the band, which are not adjusted, keep tracking the same cells.
    /// </summary>
    private void AdjustDependentsForBandShift(IRegion affectedRegion, string sheetName,
        DependencyManagerRestoreData restoreData, Action<CellFormula> adjust)
    {
        var dependents = GetDirectDependents(affectedRegion, sheetName).Distinct().ToList();

        foreach (var dependent in dependents)
        {
            var formula = dependent.Formula;
            if (formula == null)
                continue;

            restoreData.CaptureReferences(formula);
            RemoveReferenceTrackingForFormula(dependent, restoreData);
            adjust(formula);

            foreach (var formulaRef in formula.References)
            {
                // structured references were untracked before the shift and are re-tracked after it
                if (formulaRef.Kind is ReferenceKind.Named or ReferenceKind.Structured || formulaRef.IsInvalid)
                    continue;

                restoreData.MergeRegionRestoreData(
                    formulaRef.SheetName,
                    GetReferencedVertexStore(formulaRef.SheetName).Add(formulaRef.Region.Clone(), dependent));
            }
        }
    }

    /// <summary>
    /// Shifts the formula vertices in <paramref name="affectedRegion"/> by <paramref name="amount"/> along
    /// <paramref name="axis"/>, shifts the formula location store with <paramref name="shiftLocations"/> and
    /// relinks the outgoing edges of the moved vertices.
    /// </summary>
    private void ShiftVerticesInBand(IRegion affectedRegion, Axis axis, int amount, string sheetName,
        DependencyManagerRestoreData restoreData, Func<RegionRestoreData<FormulaVertex>> shiftLocations)
    {
        var moved = GetVerticesInRegion(affectedRegion, sheetName).ToList();
        // edges are restored after the vertices have been shifted back, so they are recorded with the old keys
        var oldKeys = moved.ToDictionary<FormulaVertex, FormulaVertex, VertexKey>(v => v, v => v.Key, ReferenceEqualityComparer.Instance);

        var (dRow, dCol) = GetShiftDelta(axis, amount);
        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol, sheetName));
        restoreData.MergeFormulaLocationRestoreData(sheetName, shiftLocations());

        RelinkOutgoingEdges(moved, oldKeys, restoreData);
    }

    /// <summary>
    /// After a band-limited shift a vertex can move into or out of a referenced range that was not adjusted
    /// (because it extends outside the band), so the edges to its dependents are recomputed from its new position.
    /// </summary>
    private void RelinkOutgoingEdges(IEnumerable<FormulaVertex> moved, Dictionary<FormulaVertex, VertexKey> oldKeys,
        DependencyManagerRestoreData restoreData)
    {
        VertexKey KeyBeforeShift(FormulaVertex v) => oldKeys.TryGetValue(v, out var key) ? key : v.Key;

        foreach (var vertex in moved)
        {
            var current = _dependencyGraph.GetDependentsOf(vertex).ToList();
            var expected = GetDirectDependents(vertex).Distinct().ToList();

            foreach (var dependent in current)
            {
                if (expected.Contains(dependent))
                    continue;
                _dependencyGraph.RemoveEdge(vertex, dependent, false);
                restoreData.EdgesRemoved.Add((KeyBeforeShift(vertex), KeyBeforeShift(dependent)));
            }

            foreach (var dependent in expected)
            {
                if (current.Contains(dependent))
                    continue;
                _dependencyGraph.AddEdge(vertex, dependent);
                restoreData.EdgesAdded.Add((KeyBeforeShift(vertex), KeyBeforeShift(dependent)));
            }
        }
    }

    private DependencyManagerRestoreData ShiftVerticesInRegion(IRegion region, int dRow, int dCol, string sheetName)
    {
        var restoreData = new DependencyManagerRestoreData();
        // shift any affected vertices by the number inserted
        // shift the furthest vertices first, so that a vertex never moves onto the key of one yet to move
        var affectedVertices = GetVerticesInRegion(region, sheetName)
            .OrderByDescending(v => v.Row * Math.Sign(dRow) + v.Col * Math.Sign(dCol))
            .ToList();
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
    public IList<IList<FormulaVertex>> GetCalculationOrder(IEnumerable<FormulaVertex>? dirtyFormula = null,
        bool includeVolatiles = true)
    {
        if (dirtyFormula == null)
            return _sccSort.Sort();

        return _sccSort.Sort(includeVolatiles ? ConcatVolatiles(dirtyFormula) : dirtyFormula);
    }

    /// <summary>
    /// Whether <see cref="GetCalculationOrder"/> would find anything to do for the given dirty set.
    /// </summary>
    /// <remarks>
    /// Callers ask before sorting so that a write to a sheet with no formulas on it costs nothing.
    /// </remarks>
    public bool HasAnythingToCalculate(IReadOnlyCollection<FormulaVertex>? dirtyFormula)
    {
        if (dirtyFormula == null)
            return _dependencyGraph.Count > 0;

        return dirtyFormula.Count > 0 || _volatileVertices.Count > 0;
    }

    /// <summary>
    /// Yields the dirty vertices followed by the volatile ones, without the enumerator chain that
    /// LINQ Concat builds - this runs on every recalculation.
    /// </summary>
    private IEnumerable<FormulaVertex> ConcatVolatiles(IEnumerable<FormulaVertex> dirtyFormula)
    {
        foreach (var vertex in dirtyFormula)
            yield return vertex;

        foreach (var vertex in _volatileVertices)
            yield return vertex;
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
        RestoreFormulaLocationStores(restoreData);
        RestoreModifiedFormulaReferences(restoreData);
        RestoreTableDependents(restoreData);
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

            if (formulaRef is StructuredReference structuredRef)
            {
                TrackStructuredReference(formulaVertex, structuredRef, restoreData);
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

        IndexVertex(formulaVertex);
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
            // only the one entry - another reference of the formula may track the same region
            StructuredReference { IsResolved: true } structuredRef => GetReferencedVertexStore(structuredRef.SheetName)
                .GetDataRegions(structuredRef.Region, formulaVertex)
                .Take(1)
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

            var affectedRegion = GetAffectedRegion(shift.Axis, shift.Index, shift.Band);
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
        // keep the vertices even if they are left without edges - they still hold formulas
        foreach (var edge in restoreData.EdgesAdded)
            _dependencyGraph.RemoveEdge(edge.Item1, edge.Item2, false);

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

    private void RestoreFormulaLocationStores(DependencyManagerRestoreData restoreData)
    {
        foreach (var (sheetName, sheetRestoreData) in restoreData.FormulaLocationRestoreDataBySheet)
        {
            if (_formulaLocations.TryGetValue(sheetName, out var store))
                store.Restore(sheetRestoreData);
        }
    }

    private static void RestoreModifiedFormulaReferences(DependencyManagerRestoreData restoreData)
    {
        foreach (var regionModification in restoreData.ModifiedFormulaReferences)
            RestoreFormulaReferences(regionModification);
    }

    /// <summary>
    /// Brings the table index back in line with the restored vertices and references.
    /// </summary>
    private void RestoreTableDependents(DependencyManagerRestoreData restoreData)
    {
        var touched = restoreData.VerticesAdded
            .Concat(restoreData.VerticesRemoved)
            .Concat(restoreData.ModifiedFormulaReferences.Select(x => x.Vertex).OfType<FormulaVertex>());

        foreach (var vertex in touched)
        {
            if (IsInGraph(vertex))
                IndexVertex(vertex);
            else
                UnindexVertex(vertex);
        }
    }

    internal static ReferenceRestoreData CaptureReferenceRestoreData(CellFormula formula, FormulaVertex? vertex = null)
    {
        var existingRegions = formula.References.Select(r => r.Region.Clone()).ToList();
        var existingValidities = formula.References.Select(r => r.IsInvalid).ToList();
        var sheetNames = formula.References.Select(x => x.SheetName).ToList();
        var explicitSheetNames = formula.References.Select(x => x.ExplicitSheetName).ToList();
        var resolvedTableNames = formula.References.Select(x => (x as StructuredReference)?.ResolvedTableName).ToList();
        return new ReferenceRestoreData(formula, existingRegions, existingValidities, sheetNames, explicitSheetNames)
        {
            ResolvedTableNames = resolvedTableNames,
            Vertex = vertex
        };
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
            if (formulaReference is StructuredReference structuredRef)
                structuredRef.ResolvedTableName = regionModification.ResolvedTableNames?[refIndex];
            refIndex++;
        }
    }

    private static IRegion GetAffectedRegion(Axis axis, int index)
    {
        return axis == Axis.Col
            ? new ColumnRegion(index, int.MaxValue)
            : new RowRegion(index, int.MaxValue);
    }

    /// <summary>
    /// The region shifted by a shift at <paramref name="index"/>. When <paramref name="band"/> is given, only the
    /// half-band of rows (<see cref="Axis.Col"/>) or columns (<see cref="Axis.Row"/>) it covers is shifted.
    /// </summary>
    private static IRegion GetAffectedRegion(Axis axis, int index, IRegion? band)
    {
        if (band == null)
            return GetAffectedRegion(axis, index);

        return axis == Axis.Col
            ? new Region(band.Top, band.Bottom, index, int.MaxValue)
            : new Region(index, int.MaxValue, band.Left, band.Right);
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

    /// <summary>
    /// The vertices whose formulas have structured references to the table <paramref name="tableName"/>,
    /// including unqualified references (e.g. [@Col]) resolved against it.
    /// </summary>
    public IEnumerable<FormulaVertex> GetTableDependents(string tableName)
    {
        return _tableDependents.TryGetValue(tableName, out var dependents)
            ? dependents.ToList()
            : Array.Empty<FormulaVertex>();
    }

    /// <summary>
    /// Re-resolves the structured references of the formulas that depend on the table <paramref name="tableName"/>
    /// (and of unqualified references that are not in a table), updating the regions they track. Call after a
    /// table is added, removed, renamed or resized.
    /// </summary>
    /// <param name="tableName">The table, or null for every structured reference.</param>
    /// <returns>The vertices refreshed - every one that depended on the table before or after.</returns>
    public IReadOnlyList<FormulaVertex> RefreshStructuredReferences(string? tableName = null)
    {
        var vertices = tableName == null
            ? _tableDependentKeys.Keys.ToList()
            : new HashSet<FormulaVertex>(GetTableDependents(tableName).Concat(GetTableDependents("")),
                ReferenceEqualityComparer.Instance).ToList();

        // the tracking helpers record how to undo what they do, which a refresh has no use for
        var restoreData = new DependencyManagerRestoreData();
        var inGraph = vertices.Where(IsInGraph).ToList();

        foreach (var vertex in inGraph)
            UntrackStructuredReferences(vertex, restoreData);

        foreach (var vertex in inGraph)
            TrackStructuredReferences(vertex, restoreData);

        return vertices;
    }

    /// <summary>
    /// Renames the table in the structured references that refer to it. The formula text follows, because it
    /// is derived from the references. The tracked regions are unchanged; refresh the references once the
    /// table itself has been renamed.
    /// </summary>
    public void RenameTable(string oldName, string newName)
    {
        foreach (var vertex in GetTableDependents(oldName))
        {
            foreach (var reference in GetStructuredReferences(vertex))
            {
                if (string.Equals(reference.Descriptor.TableName, oldName, StringComparison.OrdinalIgnoreCase))
                    reference.Descriptor = reference.Descriptor.WithTableName(newName);
                if (string.Equals(reference.ResolvedTableName, oldName, StringComparison.OrdinalIgnoreCase))
                    reference.ResolvedTableName = newName;
            }

            IndexVertex(vertex);
        }
    }

    /// <summary>
    /// Renames the column <paramref name="oldColumn"/> of the table <paramref name="tableName"/> in the structured
    /// references that refer to it, including unqualified references resolved against the table. Whitespace around
    /// the names is not significant, so the references are written with the trimmed new name.
    /// </summary>
    public void RenameTableColumn(string tableName, string oldColumn, string newColumn)
    {
        oldColumn = oldColumn.Trim();
        newColumn = newColumn.Trim();

        foreach (var vertex in GetTableDependents(tableName))
        {
            foreach (var reference in GetStructuredReferences(vertex))
            {
                var referencedTable = reference.Descriptor.TableName ?? reference.ResolvedTableName;
                if (!string.Equals(referencedTable, tableName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var columnStart = RenameColumn(reference.ColumnStart);
                var columnEnd = RenameColumn(reference.ColumnEnd);
                if (columnStart != reference.ColumnStart || columnEnd != reference.ColumnEnd)
                    reference.Descriptor = reference.Descriptor.WithColumnNames(columnStart, columnEnd);
            }
        }

        string? RenameColumn(string? column) =>
            column != null && column.AsSpan().Trim().Equals(oldColumn, StringComparison.OrdinalIgnoreCase)
                ? newColumn
                : column;
    }

    /// <summary>
    /// The vertices whose structured references may resolve differently after a shift on
    /// <paramref name="sheetName"/>, with their structured references untracked, and the keys of the vertices
    /// that the shift will move. Untracked before anything is shifted because tracked regions are only removed
    /// on an exact match.
    /// </summary>
    private TableShift? UntrackStructuredReferencesForShift(IRegion affectedRegion, Axis axis, string sheetName,
        DependencyManagerRestoreData restoreData)
    {
        // tracked regions ending just before an insert are expanded by the referenced vertex store, so references
        // resolved to them are retracked too, to keep the tracking matching the table
        var touchedRegion = affectedRegion.Clone();
        if (touchedRegion.GetLeadingEdgeOffset(axis) > 0)
            touchedRegion.Expand(axis == Axis.Row ? Edge.Top : Edge.Left, 1);

        var dependents = _tableDependentKeys.Keys
            .Where(v => IsAffectedByShift(v, affectedRegion, touchedRegion, sheetName))
            .ToList();

        if (dependents.Count == 0)
            return null;

        // edges added after the shift are restored after the vertices have been shifted back,
        // so they are recorded with the keys from before the shift
        var keysBeforeShift = new Dictionary<FormulaVertex, VertexKey>(ReferenceEqualityComparer.Instance);
        foreach (var vertex in GetVerticesInRegion(affectedRegion, sheetName))
            keysBeforeShift.TryAdd(vertex, vertex.Key);

        foreach (var dependent in dependents)
            UntrackStructuredReferences(dependent, restoreData);

        return new TableShift(dependents, keysBeforeShift);
    }

    /// <summary>
    /// Re-tracks the structured references untracked by <see cref="UntrackStructuredReferencesForShift"/>,
    /// resolving them against the shifted tables from the shifted vertex positions.
    /// </summary>
    private void RetrackStructuredReferencesAfterShift(TableShift? tableShift, DependencyManagerRestoreData restoreData)
    {
        if (tableShift == null)
            return;

        foreach (var dependent in tableShift.Dependents)
        {
            // the formula may have been removed with the cells
            if (IsInGraph(dependent))
                TrackStructuredReferences(dependent, restoreData, tableShift.KeysBeforeShift);
        }
    }

    /// <summary>
    /// Whether a shift of the cells in <paramref name="affectedRegion"/> on <paramref name="sheetName"/> may change
    /// what the vertex's structured references resolve to, or how they are tracked: the vertex itself moves
    /// (relevant to [@Col] and unqualified references), or one of its resolved regions intersects
    /// <paramref name="touchedRegion"/> - the affected region plus the row or column before it. A resolved region
    /// is part of its table bounded by the table's edges, and a table grows only when cells are inserted inside it,
    /// so a region clear of the shift resolves the same after it. Unresolved references are retried too - e.g. a
    /// table emptied by a remove comes back on undo.
    /// </summary>
    private static bool IsAffectedByShift(FormulaVertex vertex, IRegion affectedRegion, IRegion touchedRegion,
        string sheetName)
    {
        if (vertex.VertexType == VertexType.Cell && vertex.SheetName == sheetName &&
            affectedRegion.Contains(vertex.Row, vertex.Col))
            return true;

        foreach (var reference in GetStructuredReferences(vertex))
        {
            if (!reference.IsResolved ||
                (reference.SheetName == sheetName && reference.Region.Intersects(touchedRegion)))
                return true;
        }

        return false;
    }

    private void TrackStructuredReferences(FormulaVertex vertex, DependencyManagerRestoreData restoreData,
        Dictionary<FormulaVertex, VertexKey>? keysBeforeShift = null)
    {
        foreach (var reference in GetStructuredReferences(vertex))
            TrackStructuredReference(vertex, reference, restoreData, keysBeforeShift);

        IndexVertex(vertex);
    }

    private void UntrackStructuredReferences(FormulaVertex vertex, DependencyManagerRestoreData restoreData)
    {
        if (vertex.Formula == null)
            return;

        restoreData.CaptureReferences(vertex.Formula, vertex);

        foreach (var reference in GetStructuredReferences(vertex))
            UntrackStructuredReference(vertex, reference, restoreData);

        IndexVertex(vertex);
    }

    /// <summary>
    /// Resolves <paramref name="reference"/> from the vertex's position and, if it resolves to cells, tracks
    /// that region and adds edges from the formulas in it. A reference that resolves to values only (e.g. the
    /// headers of a sheet table, which are column headings) has no cells to track, so it is left unresolved;
    /// cell edits can't change it.
    /// </summary>
    private void TrackStructuredReference(FormulaVertex vertex, StructuredReference reference,
        DependencyManagerRestoreData restoreData, Dictionary<FormulaVertex, VertexKey>? keysBeforeShift = null)
    {
        ResetStructuredReference(reference);
        if (_resolver == null)
            return;

        var isCell = vertex.VertexType == VertexType.Cell;
        var resolution = _resolver.Resolve(reference.Descriptor,
            isCell ? vertex.Row : null,
            isCell ? vertex.Col : null,
            isCell ? vertex.SheetName : null);

        // kept even when the reference doesn't resolve to cells, so that renames find unqualified references
        reference.ResolvedTableName = resolution.TableName;

        if (resolution.IsError || resolution.Region == null || resolution.SheetName == null)
            return;

        var sheetName = resolution.SheetName;
        reference.SetRegion(resolution.Region.Clone());
        reference.SetSheetName(sheetName, true);

        VertexKey KeyBeforeShift(FormulaVertex v) =>
            keysBeforeShift != null && keysBeforeShift.TryGetValue(v, out var key) ? key : v.Key;

        foreach (var precedent in GetVerticesInRegion(reference.Region, sheetName))
        {
            // an existing edge belongs to another reference and must survive a restore
            if (_dependencyGraph.HasEdge(precedent, vertex))
                continue;

            _dependencyGraph.AddEdge(precedent, vertex);
            restoreData.EdgesAdded.Add((KeyBeforeShift(precedent), KeyBeforeShift(vertex)));
        }

        restoreData.MergeRegionRestoreData(
            sheetName,
            GetReferencedVertexStore(sheetName).Add(reference.Region.Clone(), vertex));
    }

    /// <summary>
    /// Removes the tracking of <paramref name="reference"/> and the edges from the formulas in its region that no
    /// other reference of the formula covers (a stale edge would report a false circular reference), and resets it
    /// to unresolved.
    /// </summary>
    private void UntrackStructuredReference(FormulaVertex vertex, StructuredReference reference,
        DependencyManagerRestoreData restoreData)
    {
        if (reference.IsResolved)
        {
            var tracked = GetTrackedRegionsForReference(reference, vertex);
            if (tracked.Count > 0)
            {
                restoreData.MergeRegionRestoreData(
                    reference.SheetName,
                    GetReferencedVertexStore(reference.SheetName).Delete(tracked));
            }

            foreach (var precedent in GetVerticesInRegion(reference.Region, reference.SheetName))
            {
                if (!_dependencyGraph.HasEdge(precedent, vertex) ||
                    IsCoveredByAnotherReference(vertex, reference, precedent))
                    continue;

                _dependencyGraph.RemoveEdge(precedent, vertex, false);
                restoreData.EdgesRemoved.Add((precedent.Key, vertex.Key));
            }
        }

        ResetStructuredReference(reference);
    }

    private static bool IsCoveredByAnotherReference(FormulaVertex vertex, Reference reference, FormulaVertex precedent)
    {
        foreach (var other in vertex.Formula!.References)
        {
            if (ReferenceEquals(other, reference) || other.Kind == ReferenceKind.Named || other.IsInvalid)
                continue;

            // an unresolved structured reference has an empty region, so covers nothing
            if (other.SheetName == precedent.SheetName && other.Region.Contains(precedent.Row, precedent.Col))
                return true;
        }

        return false;
    }

    private static void ResetStructuredReference(StructuredReference reference)
    {
        reference.SetRegion(new EmptyRegion());
        reference.ResolvedTableName = null;
    }

    private static IEnumerable<StructuredReference> GetStructuredReferences(FormulaVertex vertex)
    {
        return vertex.Formula?.References.OfType<StructuredReference>() ?? [];
    }

    private static string GetTableIndexKey(StructuredReference reference)
    {
        return reference.Descriptor.TableName ?? reference.ResolvedTableName ?? "";
    }

    /// <summary>
    /// (Re)indexes the vertex under the tables its structured references refer to.
    /// </summary>
    private void IndexVertex(FormulaVertex vertex)
    {
        UnindexVertex(vertex);

        var keys = GetStructuredReferences(vertex)
            .Select(GetTableIndexKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keys.Count == 0)
            return;

        _tableDependentKeys[vertex] = keys;
        foreach (var key in keys)
        {
            if (!_tableDependents.TryGetValue(key, out var dependents))
            {
                dependents = new HashSet<FormulaVertex>(ReferenceEqualityComparer.Instance);
                _tableDependents[key] = dependents;
            }

            dependents.Add(vertex);
        }
    }

    /// <summary>
    /// Removes the vertex from the table index. Returns whether it was indexed.
    /// </summary>
    private bool UnindexVertex(FormulaVertex vertex)
    {
        if (!_tableDependentKeys.Remove(vertex, out var keys))
            return false;

        foreach (var key in keys)
        {
            if (_tableDependents.TryGetValue(key, out var dependents) &&
                dependents.Remove(vertex) && dependents.Count == 0)
                _tableDependents.Remove(key);
        }

        return true;
    }

    /// <summary>
    /// Whether this exact vertex (not just one with the same key) is in the graph.
    /// </summary>
    private bool IsInGraph(FormulaVertex vertex)
    {
        return ReferenceEquals(_dependencyGraph.GetVertex(vertex.Key), vertex);
    }

    private sealed record TableShift(
        List<FormulaVertex> Dependents,
        Dictionary<FormulaVertex, VertexKey> KeysBeforeShift);

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
    public Dictionary<string, RegionRestoreData<FormulaVertex>> FormulaLocationRestoreDataBySheet { get; } = new();
    public List<FormulaVertex> VerticesRemoved { get; set; } = new();
    public List<FormulaVertex> VerticesAdded { get; set; } = new();
    public readonly List<(VertexKey, VertexKey)> EdgesRemoved = new();
    public readonly List<(VertexKey, VertexKey)> EdgesAdded = new();
    public readonly List<AppliedShift> Shifts = new();
    internal readonly List<ReferenceRestoreData> ModifiedFormulaReferences = new();

    /// <summary>
    /// The formulas whose references are captured in <see cref="ModifiedFormulaReferences"/>.
    /// </summary>
    private readonly HashSet<CellFormula> _capturedFormulas = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Captures the current state of <paramref name="formula"/>'s references so they can be restored, unless
    /// they have already been captured. Captures are replayed in order, so a second capture of the same formula
    /// would restore it to the state in between rather than to the state before the change.
    /// </summary>
    /// <param name="formula"></param>
    /// <param name="vertex">The vertex holding the formula, if the table index must be updated on restore.</param>
    internal void CaptureReferences(CellFormula formula, FormulaVertex? vertex = null)
    {
        if (_capturedFormulas.Add(formula))
            ModifiedFormulaReferences.Add(DependencyManager.CaptureReferenceRestoreData(formula, vertex));
    }

    public void MergeRegionRestoreData(string sheetName, RegionRestoreData<FormulaVertex> restoreData)
    {
        if (RegionRestoreDataBySheet.TryGetValue(sheetName, out var existing))
            existing.Merge(restoreData);
        else
            RegionRestoreDataBySheet[sheetName] = restoreData;
    }

    public void MergeFormulaLocationRestoreData(string sheetName, RegionRestoreData<FormulaVertex> restoreData)
    {
        if (FormulaLocationRestoreDataBySheet.TryGetValue(sheetName, out var existing))
            existing.Merge(restoreData);
        else
            FormulaLocationRestoreDataBySheet[sheetName] = restoreData;
    }

    public void Merge(DependencyManagerRestoreData other)
    {
        foreach (var (sheetName, sheetRestoreData) in other.RegionRestoreDataBySheet)
        {
            MergeRegionRestoreData(sheetName, sheetRestoreData);
        }

        foreach (var (sheetName, sheetRestoreData) in other.FormulaLocationRestoreDataBySheet)
        {
            MergeFormulaLocationRestoreData(sheetName, sheetRestoreData);
        }

        VerticesAdded.AddRange(other.VerticesAdded);
        VerticesRemoved.AddRange(other.VerticesRemoved);
        EdgesAdded.AddRange(other.EdgesAdded);
        EdgesRemoved.AddRange(other.EdgesRemoved);
        Shifts.AddRange(other.Shifts);
        ModifiedFormulaReferences.AddRange(other.ModifiedFormulaReferences);
        foreach (var modification in other.ModifiedFormulaReferences)
            _capturedFormulas.Add(modification.Formula);
    }
}

internal class ReferenceRestoreData
{
    public CellFormula Formula { get; }
    public List<IRegion> OldRegions { get; }
    public List<bool> OldInvalidStates { get; }
    public List<string> SheetNames { get; }

    public List<bool> ExplicitSheetReferences { get; }

    /// <summary>
    /// The table each structured reference was resolved against (null for other references).
    /// </summary>
    public List<string?>? ResolvedTableNames { get; init; }

    /// <summary>
    /// The vertex holding the formula, when the table index must be updated on restore.
    /// </summary>
    public FormulaVertex? Vertex { get; init; }

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
