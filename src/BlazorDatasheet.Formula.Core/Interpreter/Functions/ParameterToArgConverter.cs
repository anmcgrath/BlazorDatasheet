using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class ParameterToArgConverter
{
    private readonly IEnvironment _environment;

    public ParameterToArgConverter(IEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="objs">The list of arguments</param>
    /// <param name="parameterDefinition"></param>
    /// <returns></returns>
    public FuncArg ToArg(object[] objs, ParameterDefinition parameterDefinition)
    {
        switch (parameterDefinition.Dimensionality)
        {
            case ParameterDimensionality.Range:
            {
                var converted = objs.Select(obj => GetRangeArg(obj, parameterDefinition.Type)).ToArray();
                if (parameterDefinition.IsRepeating)
                    return new FuncArg(converted, parameterDefinition);
                else
                    return new FuncArg(converted.First(), parameterDefinition);
            }
            default:
                var convertedScalars = objs.Select(obj => GetScalarArg(obj, parameterDefinition.Type)).ToArray();
                if (parameterDefinition.IsRepeating)
                    return new FuncArg(convertedScalars, parameterDefinition);
                else
                    return new FuncArg(convertedScalars.First(), parameterDefinition);
        }
    }

    public FuncArg ToArg(object obj, ParameterDefinition parameterDefinition)
    {
        return ToArg(new object[] { obj }, parameterDefinition);
    }

    private CellValue[][] GetRangeArg(object? obj, ParameterType parameterDefinitionType)
    {
        if (obj == null)
            return new[] { new[] { CellValue.Error(new FormulaError(ErrorType.Na)) } };

        if (obj is RangeAddress rangeAddress)
        {
            return _environment.GetRangeValues(rangeAddress);
        }

        if (obj is ColumnAddress columnAddress)
        {
            return _environment.GetRangeValues(columnAddress);
        }

        if (obj is RowAddress rowAddress)
        {
            return _environment.GetRangeValues(rowAddress);
        }

        return new[] { new[] { GetScalarArg(obj, parameterDefinitionType) } };
    }

    private CellValue GetScalarArg(object? obj, ParameterType type)
    {
        string scalarAddress = string.Empty;

        if (obj is CellAddress cellAddress)
        {
            return _environment.GetCellValue(cellAddress.Row, cellAddress.Col);
        }

        if (obj is RangeAddress or ColumnAddress or RowAddress)
            return CellValue.Error(new FormulaError(ErrorType.Ref, "Expected a single value, got a range"));

        object? convertedValue;

        switch (type)
        {
            case ParameterType.Logical:
                convertedValue = ToLogical(obj);
                break;
            case ParameterType.Number:
                convertedValue = ToNumber(obj);
                break;
            case ParameterType.Text:
                convertedValue = ToText(obj);
                break;
            default:
                convertedValue = obj;
                break;
        }

        return new CellValue(convertedValue);
    }

    private object ToLogical(object? arg)
    {
        if (arg == null)
            return false;

        if (arg is bool b)
            return b;

        if (arg is double d)
            return d != 0;

        if (bool.TryParse(arg.ToString(), out var parsedBool))
            return parsedBool;

        return new FormulaError(ErrorType.Value);
    }

    private object ToNumber(object? val)
    {
        if (val != null && val.ConvertsToNumber())
            return Convert.ToDouble(val);

        return new FormulaError(ErrorType.Value);
    }

    private object ToText(object? val)
    {
        if (val == null)
            return string.Empty;

        if (val is FormulaError e)
            return e;

        if (val is string t)
            return t;

        return val.ToString()!;
    }
}