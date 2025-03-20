using System.Globalization;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class SeparatorSettings
{
    /*
     *  | Dec_Seperator | Row | Column | Func |
        |---------------|-----|--------|------|
        | .             | ;   | ,      | ,    |
        | ,             | ;   | \      | ;    |
     */
    /// <summary>
    /// The decimal number separator e.g '.' for English, ',' for some other cultures.
    /// </summary>
    public char DecimalNumberSeparator { get; set; }

    /// <summary>
    /// The array row separator which is nearly always ';'
    /// </summary>
    public char RowSeparator { get; set; }

    /// <summary>
    /// The formula parameter separator.
    /// </summary>
    public char ListSeparator { get; set; }

    /// <summary>
    /// The array column separator.
    /// </summary>
    public char ColumnSeparator { get; set; }

    public CultureInfo CultureInfo { get; }

    /// <summary>
    /// Creates settings with the specificed <paramref name="cultureInfo"/>
    /// </summary>
    /// <param name="cultureInfo"></param>
    public SeparatorSettings(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        DecimalNumberSeparator = Convert.ToChar(cultureInfo.NumberFormat.NumberDecimalSeparator);
        ListSeparator = DecimalNumberSeparator == ',' ? ';' : ',';
        ColumnSeparator = DecimalNumberSeparator == ',' ? '\\' : ',';
        RowSeparator = ';';
    }

    /// <summary>
    /// Creates a settings with the current culture info
    /// </summary>
    public SeparatorSettings() : this(CultureInfo.CurrentCulture)
    {
    }
}