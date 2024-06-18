using System;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Test;

public class CellValueOnly : IReadOnlyCell
{
    private readonly object _value;

    public CellValueOnly(int row, int col, object value, CellValueType cellValueType)
    {
        Row = row;
        Col = col;
        _value = value;
        ValueType = cellValueType;
    }

    public T GetValue<T>()
    {
        return (T)Value!;
    }

    public object? GetValue(Type t)
    {
        return Value;
    }

    public CellFormat Format { get; } = new CellFormat();
    public string Type { get; } = "default";
    public int Row { get; }

    public int Col { get; }
    public bool IsValid => true;
    public string? Formula { get; }
    public object? Value => _value;

    public object? GetMetaData(string name)
    {
        throw new NotImplementedException();
    }

    public CellValueType ValueType { get; }
    public bool IsVisible { get; }

    public bool HasFormula() => false;
}