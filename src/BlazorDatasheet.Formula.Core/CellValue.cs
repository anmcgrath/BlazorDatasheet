using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core;

public class CellValue
{
    public object? Data { get; init; }

    public bool IsEmpty { get; private set; }
    public CellValueType ValueType { get; init; }

    public static readonly CellValue Empty = new CellValue(null);

    /// <summary>
    /// Creates a cell value type. If <paramref name="cellValueType"/> is set, this is used to determine the value type.
    /// Otherwise, the value type is determined by looking at the type of <paramref name="data"/>.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="cellValueType"></param>
    public CellValue(object? data, CellValueType? cellValueType = null)
    {
        // Set the type and trust it if the it i
        if (cellValueType.HasValue)
        {
            Data = data;
            ValueType = cellValueType.Value;
            return;
        }

        ValueType = CellValueType.Any;

        if (data == null)
        {
            Data = null;
            IsEmpty = true;
            return;
        }

        var valType = data.GetType();
        var isNullable = valType.IsNullable();
        var nullableType = System.Nullable.GetUnderlyingType(valType);

        // If object is a string then either set the value type as string or 
        // try to convert to one of the 
        if (valType == typeof(string) || (isNullable && nullableType == typeof(string)))
        {
            var converted = TryConvertFromString(data?.ToString(), out var convertedData, out var valueType);
            if (converted)
            {
                Data = convertedData;
                ValueType = valueType!.Value;
            }
            else
            {
                Data = data;
                ValueType = CellValueType.Text;
            }
        }
        else
        {
            ValueType = GetValueType(data, valType, isNullable, nullableType);
            Data = (ValueType == CellValueType.Number) ? Convert.ToDouble(data) : data;
        }
    }

    /// <summary>
    /// Whether the cell value held is an array - that is a 2d row/col array of cell values (CellValue[][])
    /// </summary>
    /// <returns></returns>
    public bool IsArray()
    {
        return ValueType == CellValueType.Array;
    }

    private bool TryConvertFromString(string? value, out object? converted, out CellValueType? valueType)
    {
        if (value == null)
        {
            converted = null;
            valueType = CellValueType.Empty;
            return false;
        }

        if (DateTime.TryParse(value, out var valDate))
        {
            valueType = CellValueType.Date;
            converted = valDate;
            return true;
        }

        if (double.TryParse(value, out var valNum))
        {
            converted = valNum;
            valueType = CellValueType.Number;
            return true;
        }

        if (bool.TryParse(value, out var valBool))
        {
            valueType = CellValueType.Logical;
            converted = valBool;
            return true;
        }

        converted = null;
        valueType = null;
        return false;
    }

    private CellValueType GetValueType(object? value, Type valType, bool isNullable, Type? nullableType)
    {
        if (value == null)
            return CellValueType.Empty;

        if (value is FormulaError)
            return CellValueType.Error;

        if (valType == typeof(string) || (isNullable && nullableType == typeof(string)))
        {
            return CellValueType.Text;
        }

        if (valType.IsNumeric() || (isNullable && nullableType.IsNumeric()))
            return CellValueType.Number;

        if (valType == typeof(bool) || (isNullable && nullableType == typeof(bool)))
            return CellValueType.Logical;

        if (valType == typeof(DateTime) || (isNullable && nullableType == typeof(DateTime)))
            return CellValueType.Date;

        return CellValueType.Any;
    }

    /// <summary>
    /// Returns the Cell's Value and attempts to cast it to T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetValue<T>()
    {
        var val = GetValue(typeof(T));
        if (val == null)
            return default(T);
        return (T)val;
    }

    /// <summary>
    /// Returns a cell's Value and converts to the type if possible
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public object? GetValue(Type type)
    {
        try
        {
            if (Data == null && ValueType == CellValueType.Text)
                return string.Empty;

            if (this.Data?.GetType() == type)
                return Data;
            else
            {
                var conversionType = type;
                if (System.Nullable.GetUnderlyingType(type) != null)
                {
                    conversionType = System.Nullable.GetUnderlyingType(type);
                }

                if (conversionType == typeof(string))
                    return Data?.ToString();

                if (Data is IConvertible)
                    return Convert.ChangeType(Data, conversionType);

                return Data;
            }
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public static CellValue Error(FormulaError err)
    {
        return new CellValue(err, CellValueType.Error);
    }

    public static CellValue Number(double num)
    {
        return new CellValue(num, CellValueType.Number);
    }

    public static CellValue Logical(bool val)
    {
        return new CellValue(val, CellValueType.Logical);
    }

    public static CellValue Text(string text)
    {
        return new CellValue(text, CellValueType.Text);
    }

    public static CellValue Array(CellValue[][] array)
    {
        return new CellValue(array, CellValueType.Array);
    }

    public static CellValue Sequence(CellValue[] sequence)
    {
        return new CellValue(sequence, CellValueType.Sequence);
    }

    public static CellValue Date(DateTime date)
    {
        return new CellValue(date, CellValueType.Date);
    }

    public bool IsError()
    {
        return ValueType == CellValueType.Error;
    }
}