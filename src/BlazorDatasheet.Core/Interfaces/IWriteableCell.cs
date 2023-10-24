namespace BlazorDatasheet.Core.Interfaces;

public interface IWriteableCell
{
    public bool TrySetValue<T>(T val);
    public bool TrySetValue(object? val, Type type);
    T GetValue<T>();
    object? GetValue(Type t);
}