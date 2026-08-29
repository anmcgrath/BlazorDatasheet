using System.Diagnostics.CodeAnalysis;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class FormulaExecutionContext
{
    private readonly Dictionary<CellFormula, CellValue> _executedValues = new();
    private readonly HashSet<CellFormula> _executing = new();
    private IList<FormulaVertex>? _currentSccGroup;
    private HashSet<CellPosition>? _currentSccCells;
    private Dictionary<string, CellFormula>? _currentSccNamedFormulas;

    public void SetCurrentGroup(IList<FormulaVertex> group)
    {
        _currentSccGroup = group;
        _currentSccCells ??= new HashSet<CellPosition>();
        _currentSccCells.Clear();
        _currentSccNamedFormulas ??= new Dictionary<string, CellFormula>(StringComparer.Ordinal);
        _currentSccNamedFormulas.Clear();
        foreach (var vertex in group)
        {
            if (vertex.Position != null)
                _currentSccCells.Add(new CellPosition(vertex.Row, vertex.Col));
            else if (vertex.Formula != null)
                _currentSccNamedFormulas[vertex.Key.Name] = vertex.Formula;
        }
    }

    internal bool IsInSccGroup(Reference reference)
    {
        if (_currentSccGroup == null)
            return false;

        if (reference is NamedReference namedRef)
            return _currentSccNamedFormulas?.ContainsKey(namedRef.Name) == true;

        var region = reference.Region;
        foreach (var cell in _currentSccCells!)
        {
            if (region.Contains(cell.row, cell.col))
                return true;
        }

        return false;
    }

    internal bool TryGetNamedFormula(string name, [NotNullWhen(true)] out CellFormula? formula)
    {
        if (_currentSccNamedFormulas != null &&
            _currentSccNamedFormulas.TryGetValue(name, out var namedFormula))
        {
            formula = namedFormula;
            return true;
        }

        formula = null;
        return false;
    }

    internal bool IsExecuting(CellFormula formula)
    {
        return _executing.Contains(formula);
    }

    public void RecordExecuted(CellFormula formula, CellValue value)
    {
        _executedValues.TryAdd(formula, value);
    }

    public bool TryGetExecutedValue(CellFormula formula, out CellValue value)
    {
        value = CellValue.Empty;
        if (_executedValues.TryGetValue(formula, out var cellValue))
        {
            value = cellValue;
            return true;
        }

        return false;
    }

    internal void SetExecuting(CellFormula formula)
    {
        _executing.Add(formula);
    }

    public void ClearExecuting()
    {
        _executing.Clear();
    }

    public void Clear()
    {
        _executing.Clear();
        _executedValues.Clear();
        _currentSccGroup = null;
        _currentSccCells?.Clear();
        _currentSccNamedFormulas?.Clear();
    }
}
