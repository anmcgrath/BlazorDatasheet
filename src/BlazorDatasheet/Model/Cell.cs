using BlazorDatasheet.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorDatasheet.Model;

public class Cell : IReadOnlyCell, IWriteableCell
{
    public string Type { get; set; } = "text";
    public Format Formatting { get; set; } = Format.Default;
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Returns the Cell's Value and attempts to cast it to T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetValue<T>()
    {
        return (T)GetValue(typeof(T));
    }

    public object? GetValue()
    {
        return GetValue<object?>();
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
            if (string.IsNullOrEmpty(Key))
            {
                if (this.Data.GetType() == type)
                    return Data;
                else
                {
                    var conversionType = type;
                    if (System.Nullable.GetUnderlyingType(type) != null)
                    {
                        conversionType = System.Nullable.GetUnderlyingType(type);
                    }

                    return Convert.ChangeType(Data, conversionType);
                }
            }

            // Use the Key!

            var val = Data?.GetType().GetProperty(Key)?.GetValue(Data, null);
            if (val.GetType() == type || System.Nullable.GetUnderlyingType(type) == val.GetType())
                return val;
            else
            {
                var conversionType = type;
                if (System.Nullable.GetUnderlyingType(type) != null)
                {
                    conversionType = System.Nullable.GetUnderlyingType(type);
                }

                return Convert.ChangeType(val, conversionType);
            }
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public bool SetValue<T>(T val)
    {
        return SetValue(val, typeof(T));
    }

    /// <summary>
    /// Sets the Cell's value and returns whether it was successful
    /// </summary>
    /// <param name="val"></param>
    /// <param name="type"></param>
    /// <returns>Whether setting the value was successful</returns>
    public bool SetValue(object? val, Type type)
    {
        try
        {
            if (string.IsNullOrEmpty(Key))
            {
                if (Data.GetType() == type)
                    Data = val;
                else
                    Data = Convert.ChangeType(val, type);
                return true;
            }

            // Set with the key
            var prop = Data.GetType().GetProperty(Key);
            if (prop == null)
                return false;

            var propType = prop.PropertyType;

            // If it's a nullable type, set propType to the underlying type
            // If the value is not null
            if (System.Nullable.GetUnderlyingType(propType) != null)
            {
                if (val == null)
                {
                    prop.SetValue(Data, null);
                    return true;
                }

                propType = System.Nullable.GetUnderlyingType(propType);
            }

            if (propType == type)
            {
                prop.SetValue(Data, val);
                return true;
            }

            object convertedValue = Convert.ChangeType(val, propType);
            prop.SetValue(Data, convertedValue);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    internal List<string> ConditionalFormattingIds { get; set; }

    public Action<object, string>? Setter { get; set; }

    public string? Key { get; set; }

    public object? Data { get; private set; }

    public Cell(object? data = null, string key = null)
    {
        Data = data;
        ConditionalFormattingIds = new List<string>();
        Key = key;
    }

    internal void AddConditionalFormat(string key)
    {
        ConditionalFormattingIds.Add(key);
    }
}