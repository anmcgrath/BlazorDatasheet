using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Serialization.Json.Converters;

internal class VariableJsonConverter : JsonConverter<Variable>
{
    public override Variable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        CellValueType valueType = CellValueType.Empty;
        JsonElement? cellValueElement = null;
        string? formula = null;
        var variableName = string.Empty;
        string? sheetName = null;

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
                    cellValueElement = JsonElement.ParseValue(ref reader);
                    break;
                case "t":
                    valueType = (CellValueType)reader.GetInt32();
                    break;
                case "f":
                    formula = reader.GetString();
                    break;
                case "sheet":
                    sheetName = reader.GetString();
                    break;
                case "n":
                    variableName = reader.GetString();
                    break;
            }
        }

        var cellValue = CellValueHelper.GetCellValue(valueType, cellValueElement);
        return new Variable(variableName!, formula, sheetName, cellValue);
    }

    public override void Write(Utf8JsonWriter writer, Variable value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("n", value.Name);

        if (value.SheetName != null)
            writer.WriteString("sheet", value.SheetName);

        if (!string.IsNullOrEmpty(value.Formula))
            writer.WriteString("f", value.Formula);
        else if (value.Value != null)
            CellValueHelper.WriteCellValue(writer, value.Value);

        writer.WriteEndObject();
    }
}