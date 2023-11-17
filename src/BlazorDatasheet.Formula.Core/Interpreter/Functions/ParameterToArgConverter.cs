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
            obj = _environment.GetCellValue(cellAddress.Row, cellAddress.Col).Data;
        }

        if (obj is RangeAddress or ColumnAddress or RowAddress)
            return CellValue.Error(new FormulaError(ErrorType.Ref, "Expected a single value, got a range"));

        CellValue? convertedValue;

        switch (type)
        {
            case ParameterType.Logical:
                if (!TryConvertToLogical(obj, out convertedValue))
                    convertedValue = CellValue.Error(new FormulaError(ErrorType.Value));
                break;
            case ParameterType.Number:
                if (!TryConvertToNumber(obj, out convertedValue))
                    convertedValue = CellValue.Error(new FormulaError(ErrorType.Value));
                break;
            case ParameterType.Text:
                if (!TryConvertToText(obj, out convertedValue))
                    convertedValue = CellValue.Error(new FormulaError(ErrorType.Value));
                break;
            default:
                convertedValue = new CellValue(obj);
                break;
        }

        return convertedValue!;
    }

    private bool TryConvertToLogical(object? arg, out CellValue? converted)
    {
        if (arg == null)
        {
            converted = new CellValue(false);
            return true;
        }

        if (arg is bool b)
        {
            converted = new CellValue(b);
            return true;
        }

        if (arg is double d)
        {
            converted = new CellValue(d != 0);
            return true;
        }

        if (bool.TryParse(arg.ToString(), out var parsedBool))
        {
            converted = new CellValue(parsedBool);
            return false;
        }

        converted = null;
        return false;
    }

    private bool TryConvertToNumber(object? val, out CellValue? converted)
    {
        if (val != null && val.ConvertsToNumber())
        {
            converted = new CellValue(Convert.ToDouble(val));
            return true;
        }

        converted = null;
        return false;
    }

    private bool TryConvertToText(object? val, out CellValue? converted)
    {
        if (val == null)
        {
            converted = CellValue.Empty;
            return true;
        }

        if (val is FormulaError e)
        {
            converted = CellValue.Error(e);
            return true;
        }

        if (val is string t)
        {
            converted = new CellValue(t);
            return true;
        }

        converted = new CellValue(val.ToString()!);
        return true;
    }
}