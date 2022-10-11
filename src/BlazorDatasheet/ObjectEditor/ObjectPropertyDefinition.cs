using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectPropertyDefinition<T>
{
    public string PropertyName { get; set; }
    public string Heading { get; set; }
    public string Type { get; set; } = "text";
    public Format Format { get; set; } = Format.Default;
    public bool IsReadOnly { get; set; }
    internal List<string> ConditionalFormatKeys { get; set; }
    internal List<IDataValidator> Validators { get; set; }

    internal ObjectPropertyDefinition(string propName, string type)
    {
        PropertyName = propName;
        Type = type;
        ConditionalFormatKeys = new List<string>();
        Validators = new List<IDataValidator>();
    }

    public void UseConditionalFormat(string key)
    {
        ConditionalFormatKeys.Add(key);
    }

    public void UseDataValidator(IDataValidator validator)
    {
        Validators.Add(validator);
    }
}