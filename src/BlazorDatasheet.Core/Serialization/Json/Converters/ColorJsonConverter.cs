using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class ColorJsonConverter : JsonConverter<System.Drawing.Color>
{
    public override System.Drawing.Color Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var htmlColor = reader.GetString();
            if (htmlColor != null)
            {
                if (htmlColor.Length == 9 && htmlColor[0] == '#' &&
                    uint.TryParse(htmlColor.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var argb))
                {
                    return System.Drawing.Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8),
                        (byte)argb);
                }

                return ColorTranslator.FromHtml(htmlColor);
            }
        }

        return new System.Drawing.Color();
    }

    public override void Write(Utf8JsonWriter writer, System.Drawing.Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.A == byte.MaxValue
            ? ColorTranslator.ToHtml(value)
            : $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}
