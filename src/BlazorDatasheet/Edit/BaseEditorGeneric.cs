namespace BlazorDatasheet.Edit;

public abstract class BaseEditor<T> : BaseEditor
{
    private T? _editedValue;

    public T? EditedValue
    {
        get => _editedValue;
        protected set
        {
            _editedValue = value;
            Console.WriteLine("BaseEditorGeneric.EditedValue set " + _editedValue);
            this.OnValueChanged.InvokeAsync(_editedValue?.ToString());
        }
    }
}