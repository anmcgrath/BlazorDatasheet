using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data;

public class SheetCell : IReadOnlyCell
{
    private readonly Sheet _sheet;

    public SheetCell(int row, int col, Sheet sheet)
    {
        Row = row;
        Col = col;
        _sheet = sheet;
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

    public CellFormat? Formatting { get; }
    public string Type { get; } = "text";
    public int Row { get; }
    public int Col { get; }
    public bool IsValid { get; }
    
    public CellFormula? Formula => _sheet.CellFormulaStore.Get(Row, Col);
    public object? Data => _sheet.CellDataStore.Get(Row, Col);

    public object? GetMetaData(string name)
    {
        return null;
    }

    public bool HasMetaData(string name)
    {
        return false;
    }
}