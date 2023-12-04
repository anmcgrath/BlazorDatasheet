using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class ParameterTypeConverter
{
    private IEnvironment _environment;

    public ParameterTypeConverter(IEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Performs an implicit conversion of a value into another type, specified by <paramref name="definition"/>
    /// See the below link as we try to follow this standard.
    /// http://docs.oasis-open.org/office/v1.2/os/OpenDocument-v1.2-os-part2.html
    /// If the conversion cannot be performed, an error is usually the result.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public CellValue ConvertVal(object value, ParameterDefinition definition)
    {
        switch (definition.Type)
        {
            case ParameterType.Any:
                if (value is CellValue)
                    return (CellValue)value;
                if (value is CellValue[])
                    return CellValue.Sequence((CellValue[])value);
                if (value is CellValue[][])
                    return CellValue.Array((CellValue[][])value);
                return new CellValue(value);
            case ParameterType.Number:
                return ToNumber(value, definition);
            case ParameterType.NumberSequence:
                return ToNumberSequence(value, definition);
            case ParameterType.Logical:
                return ToLogical(value, definition);
            case ParameterType.LogicalSequence:
                return ToLogicalSequence(value, definition);
            case ParameterType.Text:
                return ToText(value, definition);
            case ParameterType.Date:
                return ToDate(value, definition);
            case ParameterType.Array:
                return ToArray(value, definition);
            default:
                return CellValue.Error(new FormulaError(ErrorType.Value));
        }
    }

    private CellValue ToNumber(object? o, ParameterDefinition defn)
    {
        if (o is CellAddress cellRef)
            return ToNumber(_environment.GetCellValue(
                cellRef.RowStart,
                cellRef.ColStart).Data, defn);

        if (o == null)
            return new CellValue(0, CellValueType.Number);

        if (o is FormulaError)
            return new CellValue(o, CellValueType.Error);

        if (o.ConvertsToNumber())
            return new CellValue(Convert.ToDouble(o), CellValueType.Number);

        if (o is bool oBool)
            return new CellValue(oBool ? 1 : 0, CellValueType.Logical);

        if (o is string oStr)
        {
            if (double.TryParse(oStr, out var parsedStr))
                return new CellValue(parsedStr, CellValueType.Number);
            return CellValue.Error(new FormulaError(ErrorType.Value));
        }

        return CellValue.Error(new FormulaError(ErrorType.Value));
    }

    public CellValue ToDate(object? o, ParameterDefinition defn)
    {
        if (o is CellAddress cellRef)
            return ToDate(_environment.GetCellValue(
                cellRef.RowStart,
                cellRef.ColStart).Data, defn);

        if (o == null)
            return CellValue.Empty;

        if (o is FormulaError error)
            return CellValue.Error(error);

        if (o is double n)
            return CellValue.Date((new DateTime(1900, 1, 1).AddDays(n)));

        if (o is string s)
        {
            if (DateTime.TryParse(s, out var parsedDateTime))
                return CellValue.Date(parsedDateTime);
        }

        return CellValue.Error(new FormulaError(ErrorType.Value, $"Expected a data, but got {o}"));
    }

    /// <summary>
    /// Returns an array of cell values that are either number or error
    /// </summary>
    /// <param name="o"></param>
    /// <param name="defn"></param>
    /// <returns></returns>
    public CellValue ToNumberSequence(object? o, ParameterDefinition defn)
    {
        // first try convert from range then
        // if it's not range do ToNumber
        if (o is RangeAddress rangeAddress)
        {
            List<CellValue> results = new List<CellValue>();
            var range = _environment.GetRangeValues(rangeAddress);
            for (int i = 0; i < range.Length; i++)
            {
                for (int j = 0; j < range[i].Length; j++)
                {
                    if (range[i][j].ValueType == CellValueType.Number ||
                        range[i][j].ValueType == CellValueType.Error)
                    {
                        results.Add(range[i][j]);
                    }
                }
            }

            return CellValue.Sequence(results.ToArray());
        }

        return CellValue.Sequence(new[] { ToNumber(o, defn) });
    }

    public CellValue ToLogical(object? o, ParameterDefinition defn)
    {
        if (o is bool b)
            return CellValue.Logical(b);

        if (o is CellAddress cellAddress)
            return ToLogical(_environment.GetCellValue(cellAddress.RowStart, cellAddress.ColStart).Data, defn);

        if (o == null)
            return CellValue.Logical(false);

        if (o.ConvertsToNumber())
            return CellValue.Logical(Convert.ToDouble(o) != 0);

        if (o is FormulaError error)
            return CellValue.Error(error);

        if (o is string oStr)
        {
            var parsed = bool.TryParse(oStr, out var parsedBool);
            if (parsed)
                return CellValue.Logical(parsedBool);
        }

        return CellValue.Error(new FormulaError(ErrorType.Value));
    }

    public CellValue ToLogicalSequence(object? o, ParameterDefinition defn)
    {
        if (o is RangeAddress rangeAddress)
        {
            List<CellValue> results = new List<CellValue>();
            var range = _environment.GetRangeValues(rangeAddress);
            for (int i = 0; i < range.Length; i++)
            {
                for (int j = 0; j < range[i].Length; j++)
                {
                    if (range[i][j].ValueType == CellValueType.Logical ||
                        range[i][j].ValueType == CellValueType.Error)
                    {
                        results.Add(range[i][j]);
                    }

                    if (range[i][j].ValueType == CellValueType.Number)
                        results.Add(ToLogical(range[i][j].Data, defn));
                }
            }

            return CellValue.Sequence(results.ToArray());
        }

        return CellValue.Sequence(new[] { ToLogical(o, defn) });
    }

    public CellValue ToText(object? o, ParameterDefinition defn)
    {
        if (o == null)
            return CellValue.Empty;

        if (o is string s)
            return CellValue.Text(s);

        if (o is CellAddress cellAddress)
            return ToText(_environment.GetCellValue(cellAddress.RowStart, cellAddress.ColStart).Data, defn);

        return CellValue.Text(o.ToString() ?? string.Empty);
    }

    public CellValue ToArray(object? o, ParameterDefinition defn)
    {
        if (o == null)
            return CellValue.Array(Array.Empty<CellValue[]>());

        if (o is RangeAddress rangeAddress)
            return CellValue.Array(_environment.GetRangeValues(rangeAddress));

        if (o is Array arr)
        {
            if (arr.Rank == 1)
            {
                var array1d = new CellValue[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                    array1d[i] = new CellValue(arr.GetValue(i));

                return CellValue.Array(new[] { array1d });
            }

            if (arr.Rank == 2)
            {
                var arrResult = new CellValue[arr.Length][];
                for (int i = 0; i < arr.Length; i++)
                {
                    var a = arr.GetValue(i) as Array;
                    arrResult[i] = new CellValue[a!.Length];
                    for (int j = 0; j < a.Length; j++)
                        arrResult[i][j] = new CellValue(a.GetValue(j));
                }

                return CellValue.Array(arrResult);
            }
        }

        return CellValue.Array(new[] { new[] { new CellValue(o) } });
    }
}