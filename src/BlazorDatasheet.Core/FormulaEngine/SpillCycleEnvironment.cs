using System.Diagnostics.CodeAnalysis;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.FormulaEngine;

/// <summary>
/// A read-only evaluation of a potential spill cycle. Prospective descendants are circular errors;
/// formulas on the potential cycle are evaluated against those errors. Functions such as IF can then
/// discard an unused circular branch, just as they do for ordinary conditional circular references.
/// </summary>
internal sealed class SpillCycleEnvironment(
    IEnvironment source,
    DependencyManager dependencies,
    IReadOnlyList<Sheet> sheets,
    HashSet<FormulaVertex> candidates,
    FormulaVertex owner,
    IRegion target) : IEnvironment
{
    private readonly Dictionary<FormulaVertex, CellValue> _values = new();
    private readonly HashSet<FormulaVertex> _executing = new();
    private Evaluator? _evaluator;

    public bool IsCircular() => HasCircularError(Evaluate(owner));

    private CellValue Evaluate(FormulaVertex vertex)
    {
        _evaluator ??= new Evaluator(this);
        if (_values.TryGetValue(vertex, out var value))
            return value;
        if (!_executing.Add(vertex))
            return CellValue.Error(ErrorType.Circular);

        try
        {
            FormulaCallerInfo? caller = vertex.VertexType == VertexType.Cell
                ? new FormulaCallerInfo(vertex.Row, vertex.Col, vertex.SheetName)
                : null;
            value = _evaluator.Evaluate(vertex.Formula!, caller: caller);
            _values[vertex] = value;
            return value;
        }
        finally
        {
            _executing.Remove(vertex);
        }
    }

    private static bool HasCircularError(CellValue value) =>
        value.Data is FormulaError { ErrorType: ErrorType.Circular } ||
        (value.IsArray() && ((CellValue[][])value.Data!).Any(row => row.Any(HasCircularError)));

    private static CellValue ElementAt(CellValue value, int row, int col)
    {
        if (!value.IsArray())
            return value;
        var values = (CellValue[][])value.Data!;
        return row >= 0 && row < values.Length && col >= 0 && col < values[row].Length
            ? values[row][col]
            : CellValue.Empty;
    }

    public CellValue GetCellValue(int row, int col, string sheetName)
    {
        if (sheetName == owner.SheetName && target.Contains(row, col))
            return CellValue.Error(ErrorType.Circular);

        var vertex = dependencies.GetVertex(row, col, sheetName);
        if (vertex != null && candidates.Contains(vertex))
            return ElementAt(Evaluate(vertex), 0, 0);

        var sheet = sheets.FirstOrDefault(x => x.Name == sheetName);
        if (sheet?.Cells.GetSpillAnchor(row, col) is { } anchor)
        {
            vertex = dependencies.GetVertex(anchor.row, anchor.col, sheetName);
            if (vertex != null && candidates.Contains(vertex))
                return ElementAt(Evaluate(vertex), row - anchor.row, col - anchor.col);
        }

        return source.GetCellValue(row, col, sheetName);
    }

    public CellValue[][] GetRangeValues(Reference reference)
    {
        var sourceValues = source.GetRangeValues(reference);
        var values = new CellValue[sourceValues.Length][];
        for (var row = 0; row < values.Length; row++)
        {
            values[row] = new CellValue[sourceValues[row].Length];
            for (var col = 0; col < values[row].Length; col++)
                values[row][col] = GetCellValue(reference.Region.Top + row, reference.Region.Left + col,
                    reference.SheetName);
        }
        return values;
    }

    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference) =>
        GetRangeValues(reference).SelectMany(row => row).Where(value => !value.IsEmpty);

    public bool TryGetVariable(string name, out CellValue value)
    {
        var vertex = dependencies.GetVertex(name);
        if (vertex != null && candidates.Contains(vertex))
        {
            value = Evaluate(vertex);
            return true;
        }

        return source.TryGetVariable(name, out value);
    }

    public CellValue GetVariable(string name) => TryGetVariable(name, out var value) ? value : source.GetVariable(name);
    public bool VariableExists(string name) => source.VariableExists(name);
    public IEnumerable<string> GetVariableNames() => source.GetVariableNames();
    public CellFormula? GetFormula(int row, int col, string sheetName) => source.GetFormula(row, col, sheetName);
    public IRegion? GetSpillRegion(int row, int col, string sheetName) =>
        sheetName == owner.SheetName && row == owner.Row && col == owner.Col
            ? target
            : source.GetSpillRegion(row, col, sheetName);

    public bool TryGetFunction(string name, [MaybeNullWhen(false)] out FunctionDescriptor descriptor) =>
        source.TryGetFunction(name, out descriptor);

    public IEnumerable<FunctionDefinition> SearchForFunctions(string text) => source.SearchForFunctions(text);

    public void SetVariable(string name, CellValue value) => throw new NotSupportedException();
    public void ClearVariable(string name) => throw new NotSupportedException();
    public void SetCellValue(int row, int col, string sheetName, CellValue value) => throw new NotSupportedException();
    public SpillResult SetSpillResult(int row, int col, string sheetName, CellValue[][] values) =>
        throw new NotSupportedException();
}
