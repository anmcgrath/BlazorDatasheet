using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json.Converters;

internal class ConditionalFormatJsonConverter : JsonConverter<ConditionalFormatModel>
{
    public override ConditionalFormatModel? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var format = new ConditionalFormatModel();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return format;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "sqref":
                    format.RegionString = reader.GetString();
                    break;
                case "rule":
                    if (JsonElement.TryParseValue(ref reader, out var el))
                    {
                        format.Rule = el.Value.Deserialize<NumberScaleConditionalFormat>(options);
                    }

                    break;
            }
        }

        return format;
    }

    public override void Write(Utf8JsonWriter writer, ConditionalFormatModel value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("sqref", value.RegionString);

        writer.WritePropertyName("rule");
        JsonSerializer.Serialize(writer, (NumberScaleConditionalFormat)value.Rule, options);

        writer.WriteEndObject();
    }
}