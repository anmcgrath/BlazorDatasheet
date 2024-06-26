using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Test.Formula;

public class TestEnvironment : IEnvironment
{
    private Dictionary<CellPosition, CellValue> _cellValues = new();
    private Dictionary<string, ISheetFunction> _functions = new();
    private Dictionary<string, CellValue> _variables = new();

    public void SetCellValue(int row, int col, object val)
    {
        SetCellValue(new CellPosition(row, col), val);
    }

    public void SetCellValue(CellPosition position, object val)
    {
        _cellValues.TryAdd(position, new CellValue(val));
        _cellValues[position] = new CellValue(val);
    }

    public void RegisterFunction(string name, ISheetFunction functionDefinition)
    {
        var validator = new FunctionParameterValidator();
        validator.ValidateOrThrow(functionDefinition.GetParameterDefinitions());

        if (!_functions.ContainsKey(name))
            _functions.Add(name, functionDefinition);
        _functions[name] = functionDefinition;
    }

    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference)
    {
        return GetRangeValues(reference)
            .SelectMany(x => x);
    }

    public void SetVariable(string name, object variable)
    {
        SetVariable(name, new CellValue(variable));
    }

    public void SetVariable(string name, CellValue value)
    {
        if (!_variables.ContainsKey(name))
            _variables.Add(name, value);
        _variables[name] = value;
    }

    public CellValue GetCellValue(int row, int col)
    {
        var hasVal = _cellValues.TryGetValue(new CellPosition(row, col), out var val);
        if (hasVal)
            return val;
        return CellValue.Empty;
    }

    public CellValue[][] GetRangeValues(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Range)
        {
            var rangeRef = (RangeReference)reference;
            var r = reference.Region;
            return GetValuesInRange(r.Top, r.Bottom, r.Left, r.Right);
        }

        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return new[] { new[] { GetCellValue(cellRef.RowIndex, cellRef.ColIndex) } };
        }

        return Array.Empty<CellValue[]>();
    }

    private CellValue[][] GetValuesInRange(int r0, int r1, int c0, int c1)
    {
        r0 = Math.Clamp(r0, 0, RangeText.MaxRows);
        r1 = Math.Clamp(r1, 0, RangeText.MaxRows);

        c0 = Math.Clamp(c0, 0, RangeText.MaxCols);
        c1 = Math.Clamp(c1, 0, RangeText.MaxCols);

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

    public CellValue GetVariable(string variableIdentifier)
    {
        return _variables[variableIdentifier];
    }
}