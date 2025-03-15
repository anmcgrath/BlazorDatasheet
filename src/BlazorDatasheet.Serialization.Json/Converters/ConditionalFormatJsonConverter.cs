using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json.Converters;

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
            return null;

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
                case "sqref":
                    format.RegionString = reader.GetString();
                    break;
                case "ruleType":
                    ruleType = reader.GetString();
                    break;
                case "ruleOptions":
                    parsedRule = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        if (string.IsNullOrEmpty(ruleType))
            return null;

        if (parsedRule == null)
            return null;

        ConditionalFormatAbstractBase? rule = null;
        var ruleTypeDefn = GetConditionalFormatType(ruleType);

        if (ruleTypeDefn != null)
            rule = parsedRule.Value.Deserialize(ruleTypeDefn, options) as ConditionalFormatAbstractBase;

        if (rule == null)
            return null;

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
        writer.WriteString("sqref", value.RegionString);
        writer.WriteString("ruleType", value.RuleType);
        writer.WritePropertyName("ruleOptions");
        JsonSerializer.Serialize(writer, value.Rule, ruleType, options);

        writer.WriteEndObject();
    }
}