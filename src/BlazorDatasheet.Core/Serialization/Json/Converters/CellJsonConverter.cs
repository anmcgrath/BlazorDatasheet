using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Core.Serialization.Json.Constants;
using BlazorDatasheet.Core.Serialization.Models;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class CellJsonConverter : JsonConverter<CellModel>
{
    public override CellModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var cell = new CellModel();
        CellValueType? valueType = null;
        JsonElement? element = null;

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
                case JsonConstants.CellValueData:
                    element = JsonElement.ParseValue(ref reader);
                    break;
                case JsonConstants.CellValueType:
                    valueType = (CellValueType)reader.GetInt32();
                    break;
                case JsonConstants.Formula:
                    cell.Formula = reader.GetString();
                    break;
                case JsonConstants.ColumnIndex:
                    cell.ColIndex = reader.GetInt32();
                    break;
                case JsonConstants.MetaData:
                    if (JsonElement.TryParseValue(ref reader, out var el))
                    {
                        if (el.Value.ValueKind != JsonValueKind.Object)
                            throw new JsonException("Cell metadata must be a JSON object.");

                        cell.MetaData = el.Value.EnumerateObject()
                            .ToDictionary(property => property.Name,
                                property => ReadMetadataValue(property.Value));
                    }

                    break;
            }
        }

        cell.CellValue = CellValueHelper.GetCellValue(valueType, element, options);
        return cell;
    }

    private static object ReadMetadataValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number when element.TryGetInt32(out var value) => value,
            JsonValueKind.Number when element.TryGetInt64(out var value) => value,
            JsonValueKind.Number when element.TryGetUInt64(out var value) => value,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ReadMetadataValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name,
                property => ReadMetadataValue(property.Value)),
            _ => throw new JsonException($"Unsupported metadata JSON kind {element.ValueKind}.")
        };
    }

    public override void Write(Utf8JsonWriter writer, CellModel value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(JsonConstants.ColumnIndex, value.ColIndex);

        if (!string.IsNullOrEmpty(value.Formula))
            writer.WriteString(JsonConstants.Formula, value.Formula);

        if (value.MetaData.Count > 0)
        {
            writer.WritePropertyName(JsonConstants.MetaData);
            JsonSerializer.Serialize(writer, value.MetaData, options);
        }

        if (string.IsNullOrEmpty(value.Formula))
            CellValueHelper.WriteCellValue(writer, value.CellValue, options);
        writer.WriteEndObject();
    }
}
