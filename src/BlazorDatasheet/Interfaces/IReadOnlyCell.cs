using BlazorDatasheet.Render;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyCell
{
    T GetValue<T>();
    object? GetValue(Type t);
    object? GetValue();
    public Format Formatting { get; }
    public bool IsReadOnly { get; }
}