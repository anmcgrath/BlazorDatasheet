using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Metadata;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data;

public class SheetCell : IReadOnlyCell
{
    public int Row { get; }
    public int Col { get; }
    private Sheet _sheet { get; }
    public bool IsValid => _sheet.Cells.IsValid(Row, Col);

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
    /// Returns a cell's Value and converts to the type if possible
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public object? GetValue(Type type)
    {
        try
        {
            // only get the value once from the sheet
            var val = Value;
            if (val == null && type == typeof(string))
                return string.Empty;

            if (val?.GetType() == type)
                return val;
            else
            {
                var conversionType = type;
                if (System.Nullable.GetUnderlyingType(type) != null)
                {
                    conversionType = System.Nullable.GetUnderlyingType(type);
                }

                if (conversionType == typeof(string))
                    return val.ToString();

                if (val is IConvertible)
                    return Convert.ChangeType(val, conversionType);

                return val;
            }
        }
        catch (Exception e)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the merged format at the cell's position. Setting the format will
    /// merge the format with existing formats in the sheet.
    /// </summary>
    public CellFormat Format
    {
        get => _sheet.GetFormat(Row, Col);
        set => _sheet.Cells.MergeFormatImpl(new Region(Row, Col), value);
    }

    public string Type
    {
        get => _sheet.Cells.GetCellType(Row, Col);
        set => _sheet.Cells.SetCellTypeImpl(new Region(Row, Row, Col, Col), value);
    }

    public string? Formula
    {
        get => _sheet.Cells.GetFormulaString(Row, Col);
        set => _sheet.Cells.SetFormulaImpl(Row, Col, value);
    }

    public object? Value
    {
        get => _sheet.Cells.GetValue(Row, Col);
        set => _sheet.Cells.SetValueImpl(Row, Col, value);
    }

    public bool IsVisible => _sheet.IsCellVisible(Row, Col);

    public CellValueType ValueType => _sheet.Cells.GetCellValue(Row, Col).ValueType;

    public object? GetMetaData(string name)
    {
        return _sheet.Cells.GetMetaData(Row, Col, name);
    }

    public void Clear()
    {
        _sheet.Cells.ClearCellsImpl(new[] { new Region(Row, Col) });
    }

    public bool HasFormula()
    {
        return _sheet.Cells.HasFormula(Row, Col);
    }
}