using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Core.Serialization.Json.Constants;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class VariableJsonConverter : JsonConverter<Variable>
{
    public override Variable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        string? formula = null;
        var variableName = string.Empty;
        string? sheetName = null;
        CellValue? cellValue = null;

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
                case JsonConstants.CellValue:
                    cellValue = JsonSerializer.Deserialize<CellValue>(ref reader, options);
                    break;
                case JsonConstants.Formula:
                    formula = reader.GetString();
                    break;
                case JsonConstants.SheetName:
                    sheetName = reader.GetString();
                    break;
                case JsonConstants.VariableName:
                    variableName = reader.GetString();
                    break;
            }
        }

        return new Variable(variableName!, formula, sheetName, cellValue);
    }

    public override void Write(Utf8JsonWriter writer, Variable value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(JsonConstants.VariableName, value.Name);

        if (value.SheetName != null)
            writer.WriteString(JsonConstants.SheetName, value.SheetName);

        if (!string.IsNullOrEmpty(value.Formula))
            writer.WriteString(JsonConstants.Formula, value.Formula);
        else if (value.Value != null)
        {
            writer.WritePropertyName(JsonConstants.CellValue);
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        writer.WriteEndObject();
    }
}