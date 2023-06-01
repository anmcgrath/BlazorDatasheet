using BlazorDatasheet.Data;
using BlazorDatasheet.FormulaEngine.Interpreter.Functions;
using BlazorDatasheet.FormulaEngine.Interpreter.References;

namespace BlazorDatasheet.FormulaEngine;

public class Environment
{
    // only one sheet for now...
    private readonly Sheet _sheet;
    private readonly Dictionary<string, object> _variables = new();
    private readonly Dictionary<string, CallableFunctionDefinition> _functions = new();

    public Environment(Sheet sheet)
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

    public object? GetCellValue(int row, int col) => _sheet.GetValue(row, col);

    public BRange GetRange(int rowStart, int rowEnd, int colStart, int colEnd) =>
        _sheet.Range(rowStart, rowEnd, colStart, colEnd);

    public BRange GetColRange(ColReference start, ColReference end) =>
        _sheet.Range(new ColumnRegion(start.ColNumber, end.ColNumber));

    public BRange GetRowRange(RowReference start, RowReference end) =>
        _sheet.Range(new ColumnRegion(start.RowNumber, end.RowNumber));
}