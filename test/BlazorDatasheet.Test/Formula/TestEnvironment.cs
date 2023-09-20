using System.Collections.Generic;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.FormulaEngine;

namespace BlazorDatasheet.Test.Formula;

public class TestEnvironment : IEnvironment
{
    private Dictionary<(int row, int col), object> _cellValues = new();
    private Dictionary<string, CallableFunctionDefinition> _functions = new();
    private Dictionary<string, object> _variables = new();

    public void SetCellValue(int row, int col, object val)
    {
        if (!_cellValues.ContainsKey((row, col)))
            _cellValues.Add((row, col), val);
        _cellValues[(row, col)] = val;
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
        return _cellValues[(row, col)];
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