using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyCell
{
    T GetValue<T>();
    object? GetValue(Type t);
    object? GetValue();
    public CellFormat? Formatting { get; }
    public string Type { get; }
    public int Row { get; }
    public int Col { get; }
    public bool IsValid { get; }
    public object? Data { get; }
    object? GetMetaData(string name);
    bool HasMetaData(string name);
}