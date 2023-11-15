using System.Globalization;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Cells;

public class CellValue
{
    public object? Data { get; init; }

    public bool IsEmpty { get; private set; }
    public CellValueType ValueType { get; init; }

    private CellValue()
    {
    }

    public CellValue(object? data, CellValueType cellValueType = CellValueType.Any)
    {
        ValueType = cellValueType;

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
    public T GetValue<T>()
    {
        return (T)GetValue(typeof(T));
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

    public static CellValue Error(object? err)
    {
        return new CellValue()
        {
            Data = err,
            ValueType = CellValueType.Error
        };
    }
}