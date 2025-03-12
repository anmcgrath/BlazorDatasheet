using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
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
        CellValueType valueType = CellValueType.Empty;
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
                case "v":
                    element = JsonElement.ParseValue(ref reader);
                    break;
                case "t":
                    valueType = (CellValueType)reader.GetInt32();
                    break;
                case "f":
                    cell.Formula = reader.GetString();
                    break;
                case "c":
                    cell.ColIndex = reader.GetInt32();
                    break;
                case "m":
                    if (JsonElement.TryParseValue(ref reader, out var el))
                    {
                        cell.MetaData = el.Value.Deserialize<Dictionary<string, object>>(options)!;
                        foreach (var kp in cell.MetaData)
                        {
                            var val = (JsonElement)kp.Value;
                            if (val.ValueKind == JsonValueKind.String)
                            {
                                cell.MetaData[kp.Key] = val.GetString();
                            }
                            else if (val.ValueKind == JsonValueKind.Number)
                            {
                                cell.MetaData[kp.Key] = val.GetDouble();
                            }
                            else if (val.ValueKind == JsonValueKind.True || val.ValueKind == JsonValueKind.False)
                            {
                                cell.MetaData[kp.Key] = val.GetBoolean();
                            }
                            else
                            {
                                throw new Exception($"Unsupported meta data type for {kp.Key} type {val.ValueKind}");
                            }
                        }
                    }

                    break;
            }
        }

        if (element != null)
        {
            switch (valueType)
            {
                case CellValueType.Number:
                    cell.CellValue = CellValue.Number(element.Value.GetDouble());
                    break;
                case CellValueType.Text:
                    cell.CellValue = CellValue.Text(element.Value.GetString() ?? string.Empty);
                    break;
                case CellValueType.Date:
                    cell.CellValue = CellValue.Date(element.Value.GetDateTime());
                    break;
                case CellValueType.Logical:
                    cell.CellValue = CellValue.Logical(element.Value.GetBoolean());
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

        if (value.MetaData.Count > 0)
        {
            writer.WritePropertyName("m");
            JsonSerializer.Serialize(writer, value.MetaData, options);
        }

        if (!value.CellValue.IsEmpty)
        {
            writer.WriteNumber("t", (int)value.CellValue.ValueType);
            writer.WritePropertyName("v");
            switch (value.CellValue.ValueType)
            {
                case CellValueType.Date:
                    writer.WriteStringValue(value.CellValue.GetValue<DateTime>());
                    break;
                case CellValueType.Logical:
                    writer.WriteBooleanValue(value.CellValue.GetValue<bool>());
                    break;
                case CellValueType.Number:
                    writer.WriteNumberValue(value.CellValue.GetValue<double>());
                    break;
                case CellValueType.Text:
                    writer.WriteStringValue(value.CellValue.GetValue<string>());
                    break;
            }
        }

        writer.WriteEndObject();
    }
}