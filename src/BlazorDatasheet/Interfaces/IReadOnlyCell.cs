using System.Xml;
using BlazorDatasheet.Model;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyCell
{
    T GetValue<T>();
    object? GetValue(Type t);
    object? GetValue();
    public Format Formatting { get; }
    public bool IsReadOnly { get; }
}