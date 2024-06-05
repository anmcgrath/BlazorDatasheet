using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.ObjectEditor;

public class ObjectPropertyBuilder<T>
{
    internal string PropertyName;
    internal string Type = "default";
    internal readonly List<ConditionalFormat> ConditionalFormats;
    internal readonly List<IDataValidator> Validators;
    internal string? Heading { get; set; } = null;

    internal CellFormat CellFormat { get; private set; }

    internal ObjectPropertyBuilder(string propName)
    {
        PropertyName = propName;
        ConditionalFormats = new List<ConditionalFormat>();
        Validators = new List<IDataValidator>();
    }

    public ObjectPropertyBuilder<T> WithConditionalFormat(ConditionalFormat format)
    {
        ConditionalFormats.Add(format);
        return this;
    }

    public ObjectPropertyBuilder<T> WithType(string type)
    {
        Type = type;
        return this;
    }

    public ObjectPropertyBuilder<T> WithDataValidator(IDataValidator validator)
    {
        Validators.Add(validator);
        return this;
    }

    internal object? GetPropertyValue(T data)
    {
        return typeof(T).GetProperty(PropertyName)?.GetValue(data);
    }

    public ObjectPropertyBuilder<T> WithHeading(string heading)
    {
        Heading = heading;
        return this;
    }

    public ObjectPropertyBuilder<T> WithFormat(CellFormat format)
    {
        CellFormat = format;
        return this;
    }

    public void SetPropertyValue<T>(T item, object value)
    {
        var prop = typeof(T).GetProperty(PropertyName);
        var converted = Convert.ChangeType(value, prop.PropertyType);
        typeof(T).GetProperty(PropertyName)!.SetValue(item, converted);
    }
}