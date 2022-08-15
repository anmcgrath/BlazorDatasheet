using BlazorDatasheet.Model;

namespace BlazorDatasheet.Editor;

public class ObjectPropertyDefinition<T>
{
    public string Key { get; set; }
    public string Heading { get; set; }
    public string Type { get; set; }
    public Format Format { get; set; } = Format.Default;

    private Action<T, string>? _setter;

    public Action<T, string>? Setter
    {
        get => _setter;
        set
        {
            _setter = value;
            SetterObj = (o, s) => value.Invoke((T)o, s);
        }
    }

    internal Action<object, string>? SetterObj;
}