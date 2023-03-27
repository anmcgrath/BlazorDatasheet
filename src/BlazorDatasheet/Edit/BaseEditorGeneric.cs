namespace BlazorDatasheet.Edit;

public abstract class BaseEditor<T> : BaseEditor
{
    protected T? EditedValue { get; set; }
    public override object? GetValue() => EditedValue;
}