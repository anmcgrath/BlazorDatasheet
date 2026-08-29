using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Core.Serialization.Json.Constants;
using BlazorDatasheet.Core.Serialization.Models;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class ConditionalFormatJsonConverter : JsonConverter<ConditionalFormatModel>
{
    private readonly Dictionary<string, Type> _resolver;

    public ConditionalFormatJsonConverter(Dictionary<string, Type> resolver)
    {
        _resolver = resolver;
    }

    public override ConditionalFormatModel? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A conditional format must be a JSON object.");

        var format = new ConditionalFormatModel();
        var ruleType = string.Empty;
        JsonElement? parsedRule = null;

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
                case JsonConstants.RangeReference:
                    format.RegionString = reader.GetString();
                    break;
                case JsonConstants.ClassType:
                    ruleType = reader.GetString();
                    break;
                case JsonConstants.Options:
                    parsedRule = JsonElement.ParseValue(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (string.IsNullOrEmpty(ruleType))
            throw new JsonException("A serialized conditional format must contain a Type property.");

        if (parsedRule == null)
            throw new JsonException("A serialized conditional format must contain an Options property.");

        var ruleTypeDefn = GetConditionalFormatType(ruleType);
        if (ruleTypeDefn == null)
            throw new JsonException(
                $"Conditional format type {ruleType} is not registered in the conditional format resolver.");

        var rule = parsedRule.Value.Deserialize(ruleTypeDefn, options) as ConditionalFormatAbstractBase ??
                   throw new JsonException($"Could not deserialize conditional format type {ruleType}.");

        format.Rule = rule;

        return format;
    }

    private Type? GetConditionalFormatType(string ruleType)
    {
        if (_resolver.TryGetValue(ruleType, out var type))
            return type;

        // Default CFs 
        switch (ruleType)
        {
            case nameof(NumberScaleConditionalFormat):
                return typeof(NumberScaleConditionalFormat);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, ConditionalFormatModel value, JsonSerializerOptions options)
    {
        var ruleType = GetConditionalFormatType(value.RuleType);
        if (ruleType == null)
            throw new Exception(
                $"Could not write conditional format with rule type {value.RuleType}. Ensure it is included in the CF resolver.");

        writer.WriteStartObject();
        writer.WriteString(JsonConstants.RangeReference, value.RegionString);
        writer.WriteString(JsonConstants.ClassType, value.RuleType);
        writer.WritePropertyName(JsonConstants.Options);
        JsonSerializer.Serialize(writer, value.Rule, ruleType, options);

        writer.WriteEndObject();
    }
}
