using System.Text.Json;
using BlazorDatasheet.Core.Serialization.Json.Constants;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Core.Serialization.Json.Constants;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class CellValueHelper
{
    internal static CellValue GetCellValue(CellValueType valueType, JsonElement? valueElement)
    {
        if (valueElement != null)
        {
            switch (valueType)
            {
                case CellValueType.Number:
                    return CellValue.Number(valueElement.Value.GetDouble());
                case CellValueType.Text:
                    return CellValue.Text(valueElement.Value.GetString() ?? string.Empty);
                case CellValueType.Date:
                    return CellValue.Date(valueElement.Value.GetDateTime());
                case CellValueType.Logical:
                    return CellValue.Logical(valueElement.Value.GetBoolean());
            }
        }

        return CellValue.Empty;
    }

    internal static void WriteCellValue(Utf8JsonWriter writer, CellValue value)
    {
        if (!value.IsEmpty)
        {
            writer.WriteNumber(JsonConstants.CellValueType, (int)value.ValueType);
            writer.WritePropertyName(JsonConstants.CellValueData);
            switch (value.ValueType)
            {
                case CellValueType.Date:
                    writer.WriteStringValue(value.GetValue<DateTime>());
                    break;
                case CellValueType.Logical:
                    writer.WriteBooleanValue(value.GetValue<bool>());
                    break;
                case CellValueType.Number:
                    writer.WriteNumberValue(value.GetValue<double>());
                    break;
                case CellValueType.Text:
                    writer.WriteStringValue(value.GetValue<string>());
                    break;
            }
        }
    }
}