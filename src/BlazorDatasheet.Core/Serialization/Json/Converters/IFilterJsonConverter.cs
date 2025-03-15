using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Serialization.Json.Constants;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class IFilterJsonConverter : JsonConverter<IFilter>
{
    private readonly Dictionary<string, Type> _resolver;

    public IFilterJsonConverter(Dictionary<string, Type> resolver)
    {
        _resolver = resolver;
    }

    public override IFilter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var filterTypeString = string.Empty;
        JsonElement? parsedOptions = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case JsonConstants.ClassType:
                    filterTypeString = reader.GetString();
                    break;
                case JsonConstants.Options:
                    parsedOptions = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(filterTypeString))
            return null;

        if (parsedOptions == null)
            return null;

        var typeDefn = GetDefaultFilterType(filterTypeString);

        if (typeDefn != null)
        {
            var filter = parsedOptions.Value.Deserialize(typeDefn, options) as IFilter;
            return filter;
        }

        return null;
    }

    private Type? GetDefaultFilterType(string filterTypeString)
    {
        if (_resolver.TryGetValue(filterTypeString, out var type))
            return type;

        switch (filterTypeString)
        {
            case nameof(PatternFilter):
                return typeof(PatternFilter);
            case nameof(ValueFilter):
                return typeof(ValueFilter);
            case nameof(FilterGroup):
                return typeof(FilterGroup);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, IFilter value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        var filterTypeString = value.GetType().Name;
        var filterType = GetDefaultFilterType(filterTypeString);
        if (filterType == null)
            throw new Exception($"Serialization of filter type {filterTypeString} is not supported");

        writer.WriteString(JsonConstants.ClassType, filterTypeString);
        writer.WritePropertyName(JsonConstants.Options);
        JsonSerializer.Serialize(writer, value, filterType, options);
        writer.WriteEndObject();
    }
}