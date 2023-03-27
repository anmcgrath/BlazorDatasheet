using BlazorDatasheet.Formats;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyCell
{
    T GetValue<T>();
    object? GetValue(Type t);
    object? GetValue();
    public CellFormat? Formatting { get; }
    public bool IsReadOnly { get; }
    public string Type { get; }
    List<IDataValidator> Validators { get; }
    public int Row { get; }
    public int Col { get; }
    public bool IsValid { get; }
}