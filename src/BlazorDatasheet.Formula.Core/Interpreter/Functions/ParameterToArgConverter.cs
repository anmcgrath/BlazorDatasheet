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

        CellValue? convertedValue;

        switch (type)
        {
            case ParameterType.Logical:
                return ToLogical(obj);
            case ParameterType.Number:
                return ToNumber(obj);
            case ParameterType.Text:
                return ToText(obj);
            default:
                return new CellValue(obj);
        }

        return convertedValue;
    }

    private CellValue ToLogical(object? arg)
    {
        if (arg == null)
            return new CellValue(false);

        if (arg is bool b)
            return new CellValue(b);

        if (arg is double d)
            return new CellValue(d != 0);

        if (bool.TryParse(arg.ToString(), out var parsedBool))
            return new CellValue(parsedBool);

        return CellValue.Error(new FormulaError(ErrorType.Value));
    }

    private CellValue ToNumber(object? val)
    {
        if (val != null && val.ConvertsToNumber())
            return new CellValue(Convert.ToDouble(val));

        return CellValue.Error(new FormulaError(ErrorType.Value));
    }

    private CellValue ToText(object? val)
    {
        if (val == null)
            return CellValue.Empty;

        if (val is FormulaError e)
            return CellValue.Error(e);

        if (val is string t)
            return new CellValue(t);

        return new CellValue(val.ToString()!);
    }
}