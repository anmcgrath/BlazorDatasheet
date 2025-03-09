using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Serialization.Json.Models;

namespace BlazorDatasheet.Serialization.Json.Converters;

public class CellJsonConverter : JsonConverter<CellModel>
{
    public override CellModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var cell = new CellModel();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return cell;

            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            reader.Read();

            JsonElement? rawValue;
            CellValueType valueType;

            switch (propertyName)
            {
                case "vn":
                    cell.CellValue = CellValue.Number(reader.GetDouble());
                    break;
                case "vl":
                    cell.CellValue = CellValue.Logical(reader.GetBoolean());
                    break;
                case "vd":
                    cell.CellValue = CellValue.Date(reader.GetDateTime());
                    break;
                case "vt":
                    cell.CellValue = CellValue.Text(reader.GetString() ?? string.Empty);
                    break;
                case "f":
                    cell.Formula = reader.GetString();
                    break;
                case "c":
                    cell.ColIndex = reader.GetInt32();
                    break;
            }
        }

        return cell;
    }

    public override void Write(Utf8JsonWriter writer, CellModel value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("c", value.ColIndex);

        if (!string.IsNullOrEmpty(value.Formula))
            writer.WriteString("f", value.Formula);

        if (!value.CellValue.IsEmpty)
        {
            writer.WriteNumber("t", (int)value.CellValue.ValueType);
            switch (value.CellValue.ValueType)
            {
                case CellValueType.Date:
                    writer.WriteString("vd", value.CellValue.GetValue<DateTime>());
                    break;
                case CellValueType.Logical:
                    writer.WriteBoolean("vl", value.CellValue.GetValue<bool>());
                    break;
                case CellValueType.Number:
                    writer.WriteNumber("vn", value.CellValue.GetValue<double>());
                    break;
                case CellValueType.Text:
                    writer.WriteString("vt", value.CellValue.GetValue<string>());
                    break;
            }
        }

        writer.WriteEndObject();
    }
}