using System.Reflection;
using BlazorDatasheet.Formats;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

public class Cell : IReadOnlyCell, IWriteableCell
{
    /// <summary>
    /// The cell's row
    /// </summary>
    public int Row { get; internal set; }

    /// <summary>
    /// The cell's column
    /// </summary>
    public int Col { get; internal set; }

    /// <summary>
    /// The cell type, affects the renderer and editor used for the cell
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// The formatting to be applied to the cell on render
    /// </summary>
    public CellFormat? Formatting { get; set; }

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

    /// <summary>
    /// The cell's data, which may be a primitive or a complex object.
    /// </summary>
    public object? Data { get; private set; }

    /// <summary>
    /// The best choice for the underlying data type of Data.
    /// </summary>
    public Type DataType { get; set; }

    private Dictionary<string, object?>? _metaData;
    public IReadOnlyDictionary<string, object?> MetaData => _metaData ?? new Dictionary<string, object?>();

    /// <summary>
    /// Represents an individual datasheet cell
    /// </summary>
    /// <param name="data">The cell's data, which may be an object or a primitive</param>
    public Cell(object? data = null)
    {
        Data = data;
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
            if (Data == null && type == typeof(string))
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

    public void ClearMetadata()
    {
        _metaData?.Clear();
    }

    public void Clear()
    {
        ClearMetadata();
        ClearValue();
    }

    public void ClearValue()
    {
        var currentVal = GetValue();
        if (currentVal == null)
            return;

        Data = null;
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
        var valueSet = DoTrySetValue(val, type);
        return valueSet;
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

    internal void SetCellMetaData(string name, object? value)
    {
        if (_metaData == null)
            _metaData = new Dictionary<string, object?>();

        if (!_metaData.ContainsKey(name))
            _metaData.Add(name, value);
        _metaData[name] = value;
    }

    public object? GetMetaData(string name)
    {
        if (HasMetaData(name))
            return _metaData[name];
        return null;
    }

    public bool HasMetaData(string name)
    {
        return _metaData != null && _metaData.ContainsKey(name);
    }

    public bool DoTrySetValue(object? val, Type type)
    {
        try
        {
            if (Data == null)
                Data = val;
            else if (Data.GetType() == type) // data already of the same type
                Data = val;
            else if (val is IConvertible)
                Data = Convert.ChangeType(val, type);
            else
                Data = val;
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public void MergeFormat(CellFormat? format)
    {
        if (this.Formatting == null)
            Formatting = format.Clone();
        else
        {
            Formatting = Formatting.Clone();
            Formatting.Merge(format);
        }
    }
}