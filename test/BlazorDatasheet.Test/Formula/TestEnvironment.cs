using System.Collections.Generic;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Test.Formula;

public class TestEnvironment : IEnvironment
{
    private Dictionary<CellPosition, object> _cellValues = new();
    private Dictionary<string, CallableFunctionDefinition> _functions = new();
    private Dictionary<string, object> _variables = new();

    public void SetCellValue(int row, int col, object val)
    {
        SetCellValue(new CellPosition(row, col), val);
    }

    public void SetCellValue(CellPosition position, object val)
    {
        _cellValues.TryAdd(position, val);
        _cellValues[position] = val;
    }

    public void SetFunction(string name, CallableFunctionDefinition functionDefinition)
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

    public object GetCellValue(int row, int col)
    {
        return _cellValues[new CellPosition(row, col)];
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

    public CallableFunctionDefinition GetFunctionDefinition(string identifierText)
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