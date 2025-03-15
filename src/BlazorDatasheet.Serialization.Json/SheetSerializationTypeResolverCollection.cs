using System.Text.Json.Serialization;

namespace BlazorDatasheet.Serialization.Json;

public class SheetSerializationTypeResolverCollection
{
    public Dictionary<string, Type> ConditionalFormat { get; } = new();
    public Dictionary<string, Type> Filter { get; } = new();
    public Dictionary<string, Type> DataValidation { get; } = new();
}