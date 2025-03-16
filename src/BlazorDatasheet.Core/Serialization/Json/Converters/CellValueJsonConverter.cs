using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Core.Serialization.Json.Constants;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class CellValueJsonConverter : JsonConverter<CellValue>
{
    public override CellValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        CellValueType? cellValueType = null;
        JsonElement? cellValueElement = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            string? propertyName = reader.GetString();
            reader.Read();

            if (propertyName == JsonConstants.CellValueType)
                cellValueType = (CellValueType)reader.GetInt32()!;
            else if (propertyName == JsonConstants.CellValueData)
                cellValueElement = JsonElement.ParseValue(ref reader);
        }

        if (cellValueType == null || cellValueElement == null)
            return null;

        return CellValueHelper.GetCellValue(cellValueType.Value, cellValueElement);
    }

    public override void Write(Utf8JsonWriter writer, CellValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        CellValueHelper.WriteCellValue(writer, value);
        writer.WriteEndObject();
    }
}