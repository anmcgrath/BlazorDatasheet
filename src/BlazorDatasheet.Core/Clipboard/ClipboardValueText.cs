using System.Globalization;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Clipboard;

internal static class ClipboardValueText
{
    /// <summary>
    /// The text shown for a value when it leaves the sheet, in the current culture as the sheet parses it.
    /// </summary>
    public static string Display(CellValue value)
    {
        return value.ValueType switch
        {
            CellValueType.Empty => string.Empty,
            CellValueType.Logical => value.LogicalValue ? "TRUE" : "FALSE",
            CellValueType.Number => value.NumberValue.ToString(CultureInfo.CurrentCulture),
            _ => value.Data?.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// A culture-independent form that <see cref="TryParseInvariant"/> reads back exactly.
    /// </summary>
    public static string? Invariant(CellValue value)
    {
        return value.ValueType switch
        {
            CellValueType.Number => value.NumberValue.ToString("R", CultureInfo.InvariantCulture),
            CellValueType.Logical => value.LogicalValue ? "true" : "false",
            CellValueType.Date when value.Data is DateTime date => date.ToString("o", CultureInfo.InvariantCulture),
            CellValueType.Text => value.Data?.ToString(),
            _ => null
        };
    }

    public static string TypeName(CellValue value) => value.ValueType.ToString().ToLowerInvariant();

    public static bool TryParseInvariant(string? typeName, string? text, out CellValue value)
    {
        value = CellValue.Empty;
        if (typeName == null || text == null)
            return false;

        switch (typeName.ToLowerInvariant())
        {
            case "number":
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    return false;
                value = CellValue.Number(number);
                return true;
            case "logical":
                if (!bool.TryParse(text, out var logical))
                    return false;
                value = CellValue.Logical(logical);
                return true;
            case "date":
                if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
                    return false;
                value = CellValue.Date(date);
                return true;
            case "text":
                value = CellValue.Text(text);
                return true;
            default:
                return false;
        }
    }
}
