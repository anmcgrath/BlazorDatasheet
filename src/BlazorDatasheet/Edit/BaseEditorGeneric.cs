namespace BlazorDatasheet.Edit;

public abstract class BaseEditor<T> : BaseEditor
{
    public T? EditedValue { get; protected set; }
    public override object? GetValue() => EditedValue;
}