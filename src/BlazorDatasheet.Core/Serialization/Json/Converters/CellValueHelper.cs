using System.Text.Json;
using BlazorDatasheet.Core.Serialization.Json.Constants;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Serialization.Json.Converters;

internal class CellValueHelper
{
    internal static CellValue GetCellValue(CellValueType? valueType, JsonElement? valueElement,
        JsonSerializerOptions options)
    {
        if (valueType == null && valueElement == null)
            return CellValue.Empty;

        if (valueType == null || valueElement == null)
            throw new JsonException("A serialized cell value must contain both Type and Data properties.");

        switch (valueType)
        {
            case CellValueType.Number:
                return CellValue.Number(ReadNumber(valueElement.Value));
            case CellValueType.Text:
                return CellValue.Text(valueElement.Value.GetString() ?? string.Empty);
            case CellValueType.Date:
                return CellValue.Date(valueElement.Value.GetDateTime());
            case CellValueType.Logical:
                return CellValue.Logical(valueElement.Value.GetBoolean());
            case CellValueType.Error:
                return CellValue.Error(ReadFormulaError(valueElement.Value));
            case CellValueType.Array:
                return CellValue.Array(valueElement.Value.Deserialize<CellValue[][]>(options) ??
                                       throw new JsonException("A serialized array cell value cannot be null."));
            case CellValueType.Sequence:
                return CellValue.Sequence(valueElement.Value.Deserialize<CellValue[]>(options) ??
                                          throw new JsonException("A serialized sequence cell value cannot be null."));
            default:
                throw new JsonException($"Deserialization of cell value type {valueType} is not supported.");
        }
    }

    internal static void WriteCellValue(Utf8JsonWriter writer, CellValue value, JsonSerializerOptions options)
    {
        if (value.IsEmpty)
            return;

        if (value.ValueType is not (CellValueType.Number or CellValueType.Date or CellValueType.Text or
            CellValueType.Logical or CellValueType.Error or CellValueType.Array or CellValueType.Sequence))
        {
            throw new NotSupportedException($"Serialization of cell value type {value.ValueType} is not supported.");
        }

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
                var number = value.NumberValue;
                if (double.IsFinite(number))
                    writer.WriteNumberValue(value.GetValue<double>());
                else if (double.IsNaN(number))
                    writer.WriteStringValue("NaN");
                else if (double.IsPositiveInfinity(number))
                    writer.WriteStringValue("Infinity");
                else
                    writer.WriteStringValue("-Infinity");
                break;
            case CellValueType.Text:
                writer.WriteStringValue(value.GetValue<string>());
                break;
            case CellValueType.Error:
                var error = (FormulaError)value.Data!;
                writer.WriteStartObject();
                writer.WriteNumber(nameof(FormulaError.ErrorType), (int)error.ErrorType);
                writer.WriteString(nameof(FormulaError.Message), error.Message);
                writer.WriteEndObject();
                break;
            case CellValueType.Array:
                JsonSerializer.Serialize(writer, (CellValue[][])value.Data!, options);
                break;
            case CellValueType.Sequence:
                JsonSerializer.Serialize(writer, (CellValue[])value.Data!, options);
                break;
            default:
                throw new NotSupportedException(
                    $"Serialization of cell value type {value.ValueType} is not supported.");
        }
    }

    private static double ReadNumber(JsonElement valueElement)
    {
        if (valueElement.ValueKind == JsonValueKind.Number)
            return valueElement.GetDouble();

        if (valueElement.ValueKind == JsonValueKind.String)
        {
            return valueElement.GetString() switch
            {
                "NaN" => double.NaN,
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                _ => throw new JsonException("A string cell number must be NaN, Infinity, or -Infinity.")
            };
        }

        throw new JsonException("A serialized cell number must be a number or a named floating-point literal.");
    }

    private static FormulaError ReadFormulaError(JsonElement valueElement)
    {
        if (valueElement.ValueKind != JsonValueKind.Object ||
            !valueElement.TryGetProperty(nameof(FormulaError.ErrorType), out var errorTypeElement))
        {
            throw new JsonException("A serialized formula error must contain an ErrorType property.");
        }

        var errorType = (ErrorType)errorTypeElement.GetInt32();
        var message = valueElement.TryGetProperty(nameof(FormulaError.Message), out var messageElement)
            ? messageElement.GetString() ?? string.Empty
            : string.Empty;
        return new FormulaError(errorType, message);
    }
}
