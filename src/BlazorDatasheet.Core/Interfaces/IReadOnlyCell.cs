using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Interfaces;

public interface IReadOnlyCell
{
    T GetValue<T>();
    object? GetValue(Type t);
    public CellFormat Format { get; }
    public string Type { get; }
    public int Row { get; }
    public int Col { get; }
    public bool IsValid { get; }
    public string? Formula { get; }
    public object? Value { get; }
    object? GetMetaData(string name);
    CellValueType ValueType { get; }
    bool IsVisible { get; }
    bool HasFormula();
}