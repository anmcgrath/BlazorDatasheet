using BlazorDatasheet.FormulaEngine.Interfaces;
using BlazorDatasheet.FormulaEngine.Interpreter.Functions;
using BlazorDatasheet.FormulaEngine.Interpreter.References;

namespace BlazorDatasheet.FormulaEngine;

public class Environment
{
    // only one sheet for now...
    private readonly ISheet _sheet;
    private readonly Dictionary<string, object> _variables = new();
    private readonly Dictionary<string, CallableFunctionDefinition> _functions = new();

    public Environment(ISheet sheet)
    {
        _sheet = sheet;
    }

    public bool VariableExists(string name)
    {
        return _variables.ContainsKey(name);
    }

    public object GetVariable(string name)
    {
        return _variables[name];
    }

    public void SetVariable(string name, object value)
    {
        if (_variables.ContainsKey(name))
            _variables[name] = value;
        else
            _variables.Add(name, value);
    }

    public bool FunctionExists(string name)
    {
        return _functions.ContainsKey(name);
    }

    public CallableFunctionDefinition GetFunction(string name)
    {
        return _functions[name];
    }

    public void SetFunction(string name, CallableFunctionDefinition value)
    {
        if (_functions.ContainsKey(name))
            _functions[name] = value;
        else
            _functions.Add(name, value);
    }

    public ICell GetCell(int row, int col) => _sheet.GetCell(row, col);

    public IRange GetRange(int rowStart, int rowEnd, int colStart, int colEnd) =>
        _sheet.GetRange(rowStart, rowEnd, colStart, colEnd);

    public IRange GetColRange(ColReference start, ColReference end) =>
        _sheet.GetColumn(start.ColNumber, end.ColNumber);

    public IRange GetRowRange(RowReference start, RowReference end) =>
        _sheet.GetRowRange(start.RowNumber, end.RowNumber);
}