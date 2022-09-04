namespace BlazorDatasheet.Interfaces;

public interface IDataValidator
{
    public bool IsValid(object value);
    public bool IsStrict { get; }
}