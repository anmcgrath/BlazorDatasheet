using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Data;

public class Cell : IReadOnlyCell, IWriteableCell
{
    /// <summary>
    /// The cell type, affects the renderer and editor used for the cell
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// The formatting to be applied to the cell on render
    /// </summary>
    public Format Formatting { get; set; } = Format.Default;

    /// <summary>
    /// If IsReadOnly = true, the cell's value cannot be edited via the datasheet
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// The Data Validators to apply to a cell after editing
    /// </summary>
    public List<IDataValidator> Validators { get; private set; }

    /// <summary>
    /// Whether the Cell is in a Valid state after Data Validation
    /// </summary>
    public bool IsValid { get; internal set; } = true;

    internal List<string> ConditionalFormattingIds { get; set; }

    /// <summary>
    /// The property name that determines the cell's value from the Data, if the Data is an object
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The cell's data, which may be a primitive or a complex object.
    /// </summary>
    public object? Data { get; private set; }

    /// <summary>
    /// Represents an individual datasheet cell
    /// </summary>
    /// <param name="data">The cell's data, which may be an object or a primitive</param>
    /// <param name="key">If data is an object, the key is an optional parameter that specifies the value property name</param>
    public Cell(object? data = null, string key = null)
    {
        Data = data;
        ConditionalFormattingIds = new List<string>();
        Key = key;
        Validators = new List<IDataValidator>();
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
    /// Returns the Cell's Value
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Attempts to set the cell's value and returns whether it was successful.
    /// When this method is called directly, no events are raised by the sheet.
    /// </summary>
    /// <param name="val"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TrySetValue<T>(T val)
    {
        return TrySetValue(val, typeof(T));
    }

    /// <summary>
    /// Attempts to the Cell's value and returns whether it was successful
    /// When this method is called directly, no events are raised by the sheet.
    /// </summary>
    /// <param name="val"></param>
    /// <param name="type"></param>
    /// <returns>Whether setting the value was successful</returns>
    public bool TrySetValue(object? val, Type type)
    {
        try
        {
            if (string.IsNullOrEmpty(Key))
            {
                if (Data == null)
                    Data = val;
                else if (Data.GetType() == type)
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

    /// <summary>
    /// Associate the cell with a conditional format.
    /// </summary>
    /// <param name="key"></param>
    internal void AddConditionalFormat(string key)
    {
        ConditionalFormattingIds.Add(key);
    }
}