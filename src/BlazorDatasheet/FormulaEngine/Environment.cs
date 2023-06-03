using BlazorDatasheet.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.FormulaEngine;

public class SheetEnvironment : IEnvironment
{
    // only one sheet for now...
    private readonly Sheet _sheet;
    private readonly Dictionary<string, object> _variables = new();
    private readonly Dictionary<string, CallableFunctionDefinition> _functions = new();

    public SheetEnvironment(Sheet sheet)
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

    public CallableFunctionDefinition GetFunctionDefinition(string name)
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

    public List<double> GetNumbersInRange(RangeAddress rangeAddress)
    {
        return GetNumbersInSheetRange(
            _sheet.Range(rangeAddress.RowStart, rangeAddress.RowEnd, rangeAddress.ColStart, rangeAddress.ColEnd));
    }

    public List<double> GetNumbersInRange(ColumnAddress rangeAddress)
    {
        return GetNumbersInSheetRange(_sheet.Range(Axis.Col, rangeAddress.Start, rangeAddress.End));
    }

    public List<double> GetNumbersInRange(RowAddress rangeAddress)
    {
        return GetNumbersInSheetRange(_sheet.Range(Axis.Row, rangeAddress.Start, rangeAddress.End));
    }

    private List<double> GetNumbersInSheetRange(BRange range)
    {
        var nonEmptyCells = range.GetNonEmptyCells();
        var nums = new List<double>();
        foreach (var cell in nonEmptyCells)
        {
            var val = cell.GetValue<double?>();
            if (val != null)
                nums.Add(val.Value);
        }

        return nums;
    }
}