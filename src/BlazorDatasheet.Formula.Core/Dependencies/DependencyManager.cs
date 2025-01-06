﻿using System.Diagnostics;
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
    private readonly RegionDataStore<FormulaVertex> _referencedVertexStore = new(0, false);

    internal int FormulaCount => _dependencyGraph.Count;

    public FormulaEngineRestoreData SetFormula(string variableName, CellFormula? formula)
    {
        return SetFormula(new FormulaVertex(variableName, formula));
    }

    public FormulaEngineRestoreData SetFormula(int row, int col, CellFormula? formula)
    {
        return SetFormula(new FormulaVertex(row, col, formula));
    }

    public FormulaEngineRestoreData ClearFormula(int row, int col, CellFormula? formula = null)
    {
        return ClearFormula(new FormulaVertex(row, col, formula));
    }

    public FormulaEngineRestoreData ClearFormula(string varName, CellFormula? formula = null)
    {
        return ClearFormula(new FormulaVertex(varName, formula));
    }

    private FormulaEngineRestoreData SetFormula(FormulaVertex formulaVertex)
    {
        // Clear old formula + dependency info for restore.
        var restoreData = ClearFormula(formulaVertex);

        if (formulaVertex.Formula == null)
            return restoreData;

        _dependencyGraph.AddVertex(formulaVertex);

        return restoreData;
    }

    public void UpdateReferences(FormulaVertex formulaVertex, IEnumerable<Reference> references)
    {
        ClearDependents(formulaVertex);

        // find formula inside any of the regions that this formula references
        // and add a dependency edge to them
        foreach (var formulaRef in references)
        {
            if (formulaRef is NamedReference namedReference)
            {
                var refName = namedReference.Name;
                // handle named references
                if (_dependencyGraph.HasVertex(refName))
                {
                    var f = _dependencyGraph.GetVertex(refName);
                    _dependencyGraph.AddEdge(f, formulaVertex);
                }
            }
            else
            {
                var formulaInsideRegion = GetVerticesInRegion(formulaRef.Region);
                foreach (var f in formulaInsideRegion)
                {
                    _dependencyGraph.AddEdge(f, formulaVertex);
                }

                _referencedVertexStore.Add(formulaRef.Region.Clone(), formulaVertex);
            }
        }

        // find any formula that reference this formula and add edges to them
        if (formulaVertex.Region != null)
        {
            foreach (var dependent in GetDependents(formulaVertex.Region))
            {
                _dependencyGraph.AddEdge(formulaVertex, dependent);
            }
        }
        else if (formulaVertex.VertexType == VertexType.Named)
        {
            // TODO: Store named reference relationships to lookup direct dependents
            foreach (var vertex in _dependencyGraph.GetAll())
            {
                if (vertex.Formula?.References.Any(x => (x is NamedReference nr && nr.Name == formulaVertex.Key)) ==
                    true)
                {
                    _dependencyGraph.AddEdge(formulaVertex, vertex);
                }
            }
        }
    }

    private FormulaEngineRestoreData ClearFormula(FormulaVertex formulaVertex)
    {
        if (formulaVertex.Formula == null)
            return new FormulaEngineRestoreData();
        
        // Clear any dependency relationships.
        ClearDependents(formulaVertex);
        _dependencyGraph.RemoveVertex(formulaVertex, true);
        return new FormulaEngineRestoreData();
    }

    /// <summary>
    /// Clears any managed dependencies for the given formula vertex.
    /// </summary>
    /// <param name="formulaVertex"></param>
    private void ClearDependents(FormulaVertex formulaVertex)
    {
        // remove the link between the regions or cells that this cell references
        // and the 
        var formulaReferences = formulaVertex.Formula?.References;

        if (formulaReferences != null)
        {
            foreach (var formulaRef in formulaReferences)
            {
                var referenceStoreDataToDelete = formulaRef switch
                {
                    CellReference cellRef => GetDirectDependentData(new Region(cellRef.RowIndex, cellRef.ColIndex),
                        formulaVertex),
                    RangeReference rangeReference => GetDirectDependentData(rangeReference.Region, formulaVertex),
                    NamedReference => [],
                    _ => []
                };

                var storeDataToDelete = referenceStoreDataToDelete as DataRegion<FormulaVertex>[] ??
                                        referenceStoreDataToDelete.ToArray();

                if (storeDataToDelete.Length != 0)
                    _referencedVertexStore.Delete(storeDataToDelete);
            }
        }
    }

    public bool IsReferenced(IRegion region)
    {
        return _referencedVertexStore.Any(region);
    }

    public bool IsReferenced(int row, int col)
    {
        return _referencedVertexStore.Any(row, col) ||
               _dependencyGraph.HasVertex((new FormulaVertex(row, col, null)).Key);
    }

    /// <summary>
    /// Returns the vertices that are directly dependent on the given region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<FormulaVertex> GetDependents(IRegion region)
    {
        return _referencedVertexStore.GetData(region);
    }

    private IEnumerable<DataRegion<FormulaVertex>>
        GetDirectDependentData(IRegion region, FormulaVertex formulaVertex) =>
        _referencedVertexStore.GetDataRegions(region, formulaVertex);

    internal FormulaEngineRestoreData InsertRowAt(int row, int count) =>
        InsertRowColAt(row, count, Axis.Row);

    public FormulaEngineRestoreData InsertRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new FormulaEngineRestoreData()
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

        var formulaDependents = GetDependents(affectedRegion);

        foreach (var dependent in formulaDependents)
        {
            // capture the current references before they are modified
            var existingRegions = dependent.Formula!.References.Select(r => r.Region.Clone()).ToList();
            var existingValidities = dependent.Formula!.References.Select(r => r.IsInvalid).ToList();
            restoreData.ModifiedFormulaReferences.Add(new ReferenceRestoreData(dependent.Formula!, existingRegions,
                existingValidities));
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

    public FormulaEngineRestoreData RemoveRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new FormulaEngineRestoreData()
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
            var existingRegions = dependent.Formula!.References.Select(r => r.Region.Clone()).ToList();
            var existingValidities = dependent.Formula!.References.Select(r => r.IsInvalid).ToList();
            restoreData.ModifiedFormulaReferences.Add(new ReferenceRestoreData(dependent.Formula!, existingRegions,
                existingValidities));
            dependent.Formula!.RemoveRowColFromReferences(index, count, axis);
        }

        restoreData.Merge(ShiftVerticesInRegion(affectedRegion, dRow, dCol));
        restoreData.RegionRestoreData.Merge(_referencedVertexStore.RemoveRowColAt(index, count, axis));
        return restoreData;
    }

    private FormulaEngineRestoreData ShiftVerticesInRegion(IRegion region, int dRow, int dCol)
    {
        var restoreData = new FormulaEngineRestoreData();
        // shift any affected vertices by the number inserted
        var affectedVertices = GetVerticesInRegion(region);
        foreach (var v in affectedVertices)
        {
            // need to shift without changing the reference
            // needs to update key in dependency graph
            // and also shift the region it refers to
            v.Region!.Shift(dRow, dCol);
            _dependencyGraph.RefreshKey(v);
        }

        return restoreData;
    }

    public IEnumerable<DependencyInfo> GetDependencyInfo()
    {
        var results = new List<DependencyInfo>();
        foreach (var vertex in _dependencyGraph.GetAll())
        {
            foreach (var dependent in _dependencyGraph.Adj(vertex))
            {
                if (dependent.VertexType != VertexType.Named)
                    results.Add(new DependencyInfo(dependent.Region!, vertex.Region!, DependencyType.CalculationOrder));
            }
        }

        var dataREgions = _referencedVertexStore.GetAllDataRegions();
        foreach (var region in dataREgions)
        {
            results.Add(new DependencyInfo(region.Data.Region!, region.Region, DependencyType.Region));
        }

        return results;
    }

    public void Restore(FormulaEngineRestoreData restoreData)
    {
        foreach (var shift in restoreData.Shifts)
        {
            IRegion r = shift.Axis == Axis.Col
                ? new ColumnRegion(shift.Index, int.MaxValue)
                : new RowRegion(shift.Index, int.MaxValue);

            var dRow = shift.Axis == Axis.Row ? -shift.Amount : 0;
            var dCol = shift.Axis == Axis.Col ? -shift.Amount : 0;

            ShiftVerticesInRegion(r, dRow, dCol);
        }

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

        foreach (var regionModification in restoreData.ModifiedFormulaReferences)
        {
            int refIndex = 0;
            foreach (var formulaReference in regionModification.Formula.References)
            {
                formulaReference.SetRegion(regionModification.OldRegions[refIndex]);
                formulaReference.SetValidity(!regionModification.OldInvalidStates[refIndex]);
                refIndex++;
            }
        }
    }
}

public class FormulaEngineRestoreData
{
    public RegionRestoreData<FormulaVertex> RegionRestoreData { get; set; } = new();
    public List<FormulaVertex> VerticesRemoved { get; set; } = new();
    public List<FormulaVertex> VerticesAdded { get; set; } = new();
    public readonly List<(string, string)> EdgesRemoved = new();
    public readonly List<(string, string)> EdgesAdded = new();
    public readonly List<AppliedShift> Shifts = new();
    internal readonly List<ReferenceRestoreData> ModifiedFormulaReferences = new();

    public void Merge(FormulaEngineRestoreData other)
    {
        RegionRestoreData.Merge(other.RegionRestoreData);
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

    public ReferenceRestoreData(CellFormula formula, List<IRegion> oldRegions, List<bool> oldInvalidStates)
    {
        Formula = formula;
        OldRegions = oldRegions;
        OldInvalidStates = oldInvalidStates;
    }
}