using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorDatasheet.Serialization.Json.Converters;

public class ColorJsonConverter : JsonConverter<System.Drawing.Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var htmlColor = reader.GetString();
            if (htmlColor != null)
            {
                return ColorTranslator.FromHtml(htmlColor);
            }
        }

        return new Color();
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ColorTranslator.ToHtml(value));
    }
}