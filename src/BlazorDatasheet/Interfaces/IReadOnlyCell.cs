using System.Xml;
using BlazorDatasheet.Model;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyCell
{
    T GetValue<T>();
    object? GetValue(Type t);
    public Format Formatting { get; }
}