namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class FormulaExecutionContext
{
    private readonly Dictionary<CellFormula, CellValue> _executedValues = new();
    private readonly Stack<CellFormula> _executing = new();

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
        _executing.Push(formula);
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
    }
}