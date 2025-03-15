using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data.Filter;

namespace BlazorDatasheet.Serialization.Json.Converters;

public class IFilterJsonConverter : JsonConverter<IFilter>
{
    private readonly Func<string, Type?> _filterResolver;

    public IFilterJsonConverter(Func<string, Type?> filterResolver)
    {
        _filterResolver = filterResolver;
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
                case "type":
                    filterTypeString = reader.GetString();
                    break;
                case "options":
                    parsedOptions = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(filterTypeString))
            return null;

        if (parsedOptions == null)
            return null;

        var typeDefn = GetDefaultFilterType(filterTypeString) ?? _filterResolver(filterTypeString);

        if (typeDefn != null)
        {
            var filter = parsedOptions.Value.Deserialize(typeDefn, options) as IFilter;
            return filter;
        }

        return null;
    }

    private Type? GetDefaultFilterType(string filterTypeString)
    {
        // Default CFs 
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
        var filterType = GetDefaultFilterType(filterTypeString) ?? _filterResolver(filterTypeString);
        if (filterType == null)
            throw new Exception($"Serialization of filter type {filterTypeString} is not supported");

        writer.WriteString("type", filterTypeString);
        writer.WritePropertyName("options");
        JsonSerializer.Serialize(writer, value, filterType, options);
        writer.WriteEndObject();
    }
}