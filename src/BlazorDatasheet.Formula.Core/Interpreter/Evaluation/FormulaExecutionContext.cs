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
    private HashSet<string>? _currentSccNames;

    public void SetCurrentGroup(ref IList<FormulaVertex> group)
    {
        _currentSccGroup = group;
        _currentSccCells = new HashSet<CellPosition>();
        _currentSccNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var vertex in group)
        {
            if (vertex.Position != null)
                _currentSccCells.Add(new CellPosition(vertex.Row, vertex.Col));
            else
                _currentSccNames.Add(vertex.Key);
        }
    }

    internal bool IsInSccGroup(Reference reference)
    {
        if (_currentSccGroup == null)
            return false;

        if (reference is NamedReference namedRef)
            return _currentSccNames?.Contains(namedRef.Name) == true;

        var region = reference.Region;
        for (var row = region.Top; row <= region.Bottom; row++)
        {
            for (var col = region.Left; col <= region.Right; col++)
            {
                if (_currentSccCells?.Contains(new CellPosition(row, col)) == true)
                    return true;
            }
        }

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

    /// <summary>
    /// If the formula has been executed, returns true and sets <paramref name="value"/> to the executed value.
    /// </summary>
    /// <param name="formula"></param>
    /// <param name="value"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Clears the record of executing <see cref="CellFormula"/>
    /// </summary>
    public void ClearExecuting()
    {
        _executing.Clear();
    }

    /// <summary>
    /// Clears the execution context
    /// </summary>
    public void Clear()
    {
        _executing.Clear();
        _executedValues.Clear();
        _currentSccGroup = null;
        _currentSccCells = null;
        _currentSccNames = null;
    }
}
