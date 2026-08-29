using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class CellFormatJsonConverter : JsonConverter<CellFormat>
{
    private static readonly IReadOnlyDictionary<string, FormatProperty> PropertiesByJsonName =
        new[]
        {
            Property<string?>(nameof(CellFormat.FontWeight), (format, value) => format.FontWeight = value),
            Property<string?>(nameof(CellFormat.FontStyle), (format, value) => format.FontStyle = value),
            Property<string?>(nameof(CellFormat.TextDecoration), (format, value) => format.TextDecoration = value),
            Property<string?>(nameof(CellFormat.BackgroundColor), (format, value) => format.BackgroundColor = value),
            Property<string?>(nameof(CellFormat.ForegroundColor), (format, value) => format.ForegroundColor = value),
            Property<string?>(nameof(CellFormat.NumberFormat), (format, value) => format.NumberFormat = value),
            Property<string?>(nameof(CellFormat.Icon), (format, value) => format.Icon = value),
            Property<string?>(nameof(CellFormat.IconColor), (format, value) => format.IconColor = value),
            Property<bool?>(nameof(CellFormat.IsReadOnly), (format, value) => format.IsReadOnly = value),
            Property<TextAlign?>(nameof(CellFormat.HorizontalTextAlign),
                (format, value) => format.HorizontalTextAlign = value),
            Property<TextAlign?>(nameof(CellFormat.VerticalTextAlign),
                (format, value) => format.VerticalTextAlign = value),
            Property<Border?>(nameof(CellFormat.BorderLeft), (format, value) => format.BorderLeft = value),
            Property<Border?>(nameof(CellFormat.BorderRight), (format, value) => format.BorderRight = value),
            Property<Border?>(nameof(CellFormat.BorderTop), (format, value) => format.BorderTop = value),
            Property<Border?>(nameof(CellFormat.BorderBottom), (format, value) => format.BorderBottom = value),
            Property<TextWrapping>(nameof(CellFormat.TextWrap), (format, value) => format.TextWrap = value,
                nameof(TextWrapping))
        }.ToDictionary(x => x.JsonName);

    private static readonly IReadOnlyDictionary<string, FormatProperty> PropertiesByStyleName =
        PropertiesByJsonName.Values.ToDictionary(x => x.StyleName);

    public override CellFormat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A cell format must be a JSON object.");

        var format = new CellFormat();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return format;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a cell format property name.");

            var propertyName = reader.GetString();
            reader.Read();

            if (propertyName != null && PropertiesByJsonName.TryGetValue(propertyName, out var property))
            {
                var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
                property.SetValue(format, value);
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("Unexpected end of a cell format.");
    }

    public override void Write(Utf8JsonWriter writer, CellFormat value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Styles != null)
        {
            foreach (var style in value.Styles)
            {
                if (!PropertiesByStyleName.TryGetValue(style.Key, out var property))
                    continue;

                writer.WritePropertyName(property.JsonName);
                JsonSerializer.Serialize(writer, style.Value, property.Type, options);
            }
        }

        writer.WriteEndObject();
    }

    private static FormatProperty Property<T>(string jsonName, Action<CellFormat, T> setValue,
        string? styleName = null)
    {
        return new FormatProperty(jsonName, styleName ?? jsonName, typeof(T),
            (format, value) => setValue(format, value is null ? default! : (T)value));
    }

    private sealed record FormatProperty(string JsonName, string StyleName, Type Type,
        Action<CellFormat, object?> SetValue);
}
