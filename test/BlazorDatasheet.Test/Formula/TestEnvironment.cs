using System.Collections.Generic;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Cells;
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
        return _cellValues[new CellPosition(row, col)];
    }

    public CellValue[][] GetRangeValues(RangeAddress rangeAddress)
    {
        throw new System.NotImplementedException();
    }

    public CellValue[][] GetRangeValues(ColumnAddress rangeAddress)
    {
        throw new System.NotImplementedException();
    }

    public CellValue[][] GetRangeValues(RowAddress rangeAddress)
    {
        throw new System.NotImplementedException();
    }

    public List<double> GetNumbersInRange(RangeAddress rangeAddress)
    {
        throw new System.NotImplementedException();
    }

    public List<double> GetNumbersInRange(ColumnAddress rangeAddress)
    {
        throw new System.NotImplementedException();
    }

    public List<double> GetNumbersInRange(RowAddress rangeAddress)
    {
        throw new System.NotImplementedException();
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