using System.Globalization;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class SeparatorSettings
{
    private readonly char _rowSeparator;
    private readonly char _funcParameterSeparator;
    private readonly char _columnSeparator;

    /// <summary>
    /// The decimal number separator e.g '.' for English, ',' for some other cultures.
    /// </summary>
    public char DecimalNumberSeparator { get; set; }

    /// <summary>
    /// The array row separator which is nearly always ';'
    /// </summary>
    public char RowSeparator
    {
        get => _rowSeparator;
        init
        {
            RowSeparatorTag = CharToTag(value);
            _rowSeparator = value;
        }
    }

    internal Tag RowSeparatorTag { get; private set; }

    /// <summary>
    /// The function parameter separator.
    /// </summary>
    public char FuncParameterSeparator
    {
        get => _funcParameterSeparator;
        init
        {
            FuncParameterSeparatorTag = CharToTag(value);
            _funcParameterSeparator = value;
        }
    }

    internal Tag FuncParameterSeparatorTag { get; private set; }


    /// <summary>
    /// The array column separator.
    /// </summary>
    public char ColumnSeparator
    {
        get => _columnSeparator;
        init
        {
            ColumnSeparatorTag = CharToTag(value);
            _columnSeparator = value;
        }
    }

    internal Tag ColumnSeparatorTag { get; private set; }


    public CultureInfo CultureInfo { get; }

    /// <summary>
    /// Creates settings with the specificed <paramref name="cultureInfo"/>
    /// </summary>
    /// <param name="cultureInfo"></param>
    public SeparatorSettings(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        DecimalNumberSeparator = Convert.ToChar(cultureInfo.NumberFormat.NumberDecimalSeparator);
        FuncParameterSeparator = DecimalNumberSeparator == ',' ? ';' : ',';
        ColumnSeparator = DecimalNumberSeparator == ',' ? '\\' : ',';
        RowSeparator = ';';
    }

    /// <summary>
    /// Creates a settings with the current culture info
    /// </summary>
    public SeparatorSettings() : this(CultureInfo.CurrentCulture)
    {
    }

    private Tag CharToTag(char c)
    {
        return c switch
        {
            ';' => Tag.SemiColonToken,
            ',' => Tag.CommaToken,
            '\\' => Tag.BackslashToken,
            '.' => Tag.DotToken,
            _ => Tag.BadToken
        };
    }
}