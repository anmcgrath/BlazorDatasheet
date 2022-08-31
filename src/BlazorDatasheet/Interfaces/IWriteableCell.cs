namespace BlazorDatasheet.Interfaces;

public interface IWriteableCell
{
    public bool SetValue<T>(T val);
    public bool SetValue(object? val, Type type);
    T GetValue<T>();
    object? GetValue(Type t);
}