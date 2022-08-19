using BlazorDatasheet.Model;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectPropertyDefinition<T>
{
    public string PropertyName { get; set; }
    public string Heading { get; set; }
    public string Type { get; set; } = "text";
    public Format Format { get; set; } = Format.Default;
    public bool IsReadOnly { get; set; }
    internal List<string> ConditionalFormatKeys { get; set; }

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

    public ObjectPropertyDefinition(string propName, string type)
    {
        PropertyName = propName;
        Type = type;
        ConditionalFormatKeys = new List<string>();
    }

    public void UseConditionalFormat(string key)
    {
        ConditionalFormatKeys.Add(key);
    }
}