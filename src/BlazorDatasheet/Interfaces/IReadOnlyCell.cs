using BlazorDatasheet.DataStructures.Sheet;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Interfaces;

public interface IReadOnlyCell : ICell
{
    T GetValue<T>();
    object? GetValue(Type t);
    object? GetValue();
    public Format? Formatting { get; }
    public bool IsReadOnly { get; }
    public string Type { get; }
    List<IDataValidator> Validators { get; }
    public int Row { get; }
    public int Col { get; }
    public bool IsValid { get; }
    public string? FormulaString { get; }
    public bool HasFormula();
}