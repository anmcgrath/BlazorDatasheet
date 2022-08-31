namespace BlazorDatasheet.Interfaces;

public interface IDataValidator
{
    public bool IsValid(IReadOnlyCell cell);
    public bool IsStrict { get; }
}