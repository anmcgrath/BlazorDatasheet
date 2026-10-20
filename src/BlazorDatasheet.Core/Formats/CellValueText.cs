using System.Globalization;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Formats;

/// <summary>
/// Turns cell values into the text that the datasheet displays for them, so that anything named after a cell
/// (such as a table column named after its header cell) matches what is shown.
/// </summary>
internal static class CellValueText
{
    /// <summary>
    /// The number of decimal places that a displayed number is rounded to, unless the datasheet is given another.
    /// </summary>
    public const int DefaultNumberPrecision = 13;

    /// <summary>
    /// The text of <paramref name="value"/> as displayed with <paramref name="format"/>. A number is rounded to
    /// <paramref name="numberOfSignificantDigits"/> decimal places, then written with the format's
    /// <see cref="CellFormat.NumberFormat"/> or, without one, in the invariant culture. A date is written with the
    /// number format if there is one. Anything else, including an unformatted date, is written as its
    /// <see cref="object.ToString"/>.
    /// </summary>
    public static string ToDisplayText(CellValue value, IReadonlyCellFormat? format,
        int numberOfSignificantDigits = DefaultNumberPrecision)
    {
        var numberFormat = format?.NumberFormat;
        if (value.ValueType == CellValueType.Number)
        {
            var rounded = RoundForDisplay(value.NumberValue, numberOfSignificantDigits);
            return numberFormat != null
                ? rounded.ToString(numberFormat)
                : rounded.ToString(CultureInfo.InvariantCulture);
        }

        if (value.ValueType == CellValueType.Date && numberFormat != null && value.Data is DateTime date)
            return date.ToString(numberFormat);

        return value.Data?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Rounds <paramref name="number"/> as it is displayed, to <paramref name="numberOfSignificantDigits"/> decimal places.
    /// </summary>
    public static double RoundForDisplay(double number, int numberOfSignificantDigits) =>
        Math.Round(number, numberOfSignificantDigits);
}
