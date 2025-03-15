namespace BlazorDatasheet.Core.Serialization.Json;

public class SheetSerializationTypeResolverCollection
{
    /// <summary>
    /// Converts between a type name and the specific type of Conditional Format
    /// </summary>
    public Dictionary<string, Type> ConditionalFormat { get; } = new();

    /// Converts between a type name and the specific type of Filter
    public Dictionary<string, Type> Filter { get; } = new();

    /// Converts between a type name and the specific type of DataValidator
    public Dictionary<string, Type> DataValidation { get; } = new();

    internal SheetSerializationTypeResolverCollection()
    {
    }
}