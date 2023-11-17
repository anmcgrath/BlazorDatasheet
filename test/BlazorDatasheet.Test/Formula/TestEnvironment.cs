using System.Collections.Generic;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Test.Formula;

public class TestEnvironment : IEnvironment
{
    private Dictionary<CellPosition, CellValue> _cellValues = new();
    private Dictionary<string, ISheetFunction> _functions = new();
    private Dictionary<string, object> _variables = new();

    public void SetCellValue(int row, int col, object val)
    {
        SetCellValue(new CellPosition(row, col), val);
    }

    public void SetCellValue(CellPosition position, object val)
    {
        _cellValues.TryAdd(position, new CellValue(val));
        _cellValues[position] = new CellValue(val);
    }

    public void SetFunction(string name, ISheetFunction functionDefinition)
    {
        if (!_functions.ContainsKey(name))
            _functions.Add(name, functionDefinition);
        _functions[name] = functionDefinition;
    }

    public void SetVariable(string name, object variable)
    {
        if (!_variables.ContainsKey(name))
            _variables.Add(name, variable);
        _variables[name] = variable;
    }

    public CellValue GetCellValue(int row, int col)
    {
        var hasVal = _cellValues.TryGetValue(new CellPosition(row, col), out var val);
        if (hasVal)
            return val;
        return CellValue.Empty;
    }

    public CellValue[][] GetRangeValues(RangeAddress rangeAddress) => GetValuesInRange(rangeAddress.RowStart,
        rangeAddress.RowEnd, rangeAddress.ColStart, rangeAddress.ColEnd);

    public CellValue[][] GetRangeValues(ColumnAddress colAddress) =>
        GetValuesInRange(0, 1000, colAddress.Start, colAddress.End);

    public CellValue[][] GetRangeValues(RowAddress rowAddress) =>
        GetValuesInRange(rowAddress.Start, rowAddress.End, 0, 1000);

    private CellValue[][] GetValuesInRange(int r0, int r1, int c0, int c1)
    {
        var h = (r1 - r0) + 1;
        var w = (c1 - c0) + 1;
        var arr = new CellValue[h][];
        for (int i = 0; i < h; i++)
        {
            arr[i] = new CellValue[w];
            for (int j = 0; j < w; j++)
            {
                arr[i][j] = GetCellValue(r0 + i, c0 + j);
            }
        }

        return arr;
    }

    public bool FunctionExists(string functionIdentifier)
    {
        return _functions.ContainsKey(functionIdentifier);
    }

    public ISheetFunction GetFunctionDefinition(string identifierText)
    {
        return _functions[identifierText];
    }

    public bool VariableExists(string variableIdentifier)
    {
        return _variables.ContainsKey(variableIdentifier);
    }

    public object GetVariable(string variableIdentifier)
    {
        return _variables[variableIdentifier];
    }
}