using System.Text;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

/// <summary>
/// Parses and formats the bracketed part of structured references, e.g. the "[@Amount]" of Sales[@Amount].
/// </summary>
public static class StructuredReferenceSyntax
{
    private const char EscapeChar = '\'';

    // The order that specifiers are written in, which matches Excel.
    private static readonly TableItemSpecifier[] CanonicalOrder =
    [
        TableItemSpecifier.All,
        TableItemSpecifier.Headers,
        TableItemSpecifier.Data,
        TableItemSpecifier.Totals,
        TableItemSpecifier.ThisRow
    ];

    /// <summary>
    /// Whether <paramref name="c"/> must be escaped with ' when it is part of a column name.
    /// </summary>
    public static bool IsEscapedChar(char c) => c is '[' or ']' or '#' or '\'' or '@';

    public static string EscapeColumnName(string columnName)
    {
        var sb = new StringBuilder(columnName.Length);
        foreach (var c in columnName)
        {
            if (IsEscapedChar(c))
                sb.Append(EscapeChar);
            sb.Append(c);
        }

        return sb.ToString();
    }

    public static string UnescapeColumnName(ReadOnlySpan<char> columnName)
    {
        var sb = new StringBuilder(columnName.Length);
        for (int i = 0; i < columnName.Length; i++)
        {
            if (columnName[i] == EscapeChar && i + 1 < columnName.Length)
                i++;
            sb.Append(columnName[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses the text between the outer brackets of a structured reference.
    /// </summary>
    /// <param name="body">The text between the outer brackets, e.g "[#Headers],[Amount]" for Sales[[#Headers],[Amount]]</param>
    /// <param name="tableName">The table name before the brackets, or null if the reference is unqualified.</param>
    /// <param name="descriptor">The parsed reference, if successful.</param>
    /// <param name="error">Why the body could not be parsed, if unsuccessful.</param>
    public static bool TryParseBody(ReadOnlySpan<char> body, string? tableName,
        out StructuredReferenceDescriptor? descriptor, out string? error)
    {
        descriptor = null;
        // As in Excel, whitespace just inside the brackets of an item is not part of it, so Sales[ Amount ] is
        // Sales[Amount]. Whitespace inside a name, e.g. Sales[Unit Price], is significant.
        body = body.Trim();

        var isNested = body.Length > 0 &&
                       (body[0] == '[' ||
                        (body[0] == '@' && body[1..].TrimStart().StartsWith("[")));

        return isNested
            ? TryParseNestedBody(body, tableName, out descriptor, out error)
            : TryParseSimpleBody(body, tableName, out descriptor, out error);
    }

    /// <summary>
    /// Parses a body without inner brackets: Col, @Col, @, #Headers or nothing. The body has been trimmed.
    /// </summary>
    private static bool TryParseSimpleBody(ReadOnlySpan<char> body, string? tableName,
        out StructuredReferenceDescriptor? descriptor, out string? error)
    {
        descriptor = null;
        error = null;

        if (IndexOfUnescaped(body, '[') >= 0 || IndexOfUnescaped(body, ']') >= 0)
        {
            error = "Unexpected bracket in structured reference";
            return false;
        }

        if (body.IsEmpty)
        {
            descriptor = new StructuredReferenceDescriptor(tableName, [], null, null);
            return true;
        }

        if (body[0] == '#')
        {
            if (!TryParseSpecifier(body, out var specifier))
            {
                error = $"Unknown table specifier {body.ToString()}";
                return false;
            }

            descriptor = new StructuredReferenceDescriptor(tableName, [specifier], null, null);
            return true;
        }

        if (body[0] == '@')
        {
            var rest = body[1..].TrimStart();
            var column = rest.IsEmpty ? null : UnescapeColumnName(rest);
            descriptor = new StructuredReferenceDescriptor(tableName, [TableItemSpecifier.ThisRow], column, null);
            return true;
        }

        descriptor = new StructuredReferenceDescriptor(tableName, [], UnescapeColumnName(body), null);
        return true;
    }

    /// <summary>
    /// Parses a body made of a list of bracketed items, e.g. [#Headers],[C1]:[C2] or @[Col].
    /// </summary>
    private static bool TryParseNestedBody(ReadOnlySpan<char> body, string? tableName,
        out StructuredReferenceDescriptor? descriptor, out string? error)
    {
        descriptor = null;
        error = null;

        var pos = 0;
        var leadingThisRow = false;
        if (body[0] == '@')
        {
            leadingThisRow = true;
            pos++;
        }

        // Each item's raw text and the separator that came before it ('\0' for the first item).
        var items = new List<(string Text, char SeparatorBefore)>();
        var separator = '\0';

        while (true)
        {
            pos = SkipWhitespace(body, pos);
            if (pos >= body.Length || body[pos] != '[')
            {
                error = "Expected '[' in structured reference";
                return false;
            }

            pos++; // consume '['
            var itemStart = pos;
            while (pos < body.Length && body[pos] != ']')
            {
                if (body[pos] == '[')
                {
                    error = "Unexpected '[' in structured reference";
                    return false;
                }

                if (body[pos] == EscapeChar)
                    pos++;
                pos++;
            }

            if (pos >= body.Length)
            {
                error = "Missing ']' in structured reference";
                return false;
            }

            items.Add((body[itemStart..pos].ToString(), separator));
            pos++; // consume ']'

            pos = SkipWhitespace(body, pos);
            if (pos >= body.Length)
                break;

            separator = body[pos] switch
            {
                ',' or ';' => ',',
                ':' => ':',
                _ => '\0'
            };

            if (separator == '\0')
            {
                error = $"Unexpected '{body[pos]}' in structured reference";
                return false;
            }

            pos++; // consume separator
        }

        var specifiers = new List<TableItemSpecifier>();
        var columns = new List<string>();
        var rangeCount = 0;

        if (leadingThisRow)
            specifiers.Add(TableItemSpecifier.ThisRow);

        foreach (var item in items)
        {
            var text = item.Text.AsSpan().Trim();
            if (text.IsEmpty)
            {
                error = "Empty item in structured reference";
                return false;
            }

            if (text[0] == '#')
            {
                if (columns.Count > 0)
                {
                    error = "Table specifiers must come before columns";
                    return false;
                }

                if (leadingThisRow || item.SeparatorBefore == ':')
                {
                    error = $"Unexpected table specifier {text.ToString()}";
                    return false;
                }

                if (!TryParseSpecifier(text, out var specifier))
                {
                    error = $"Unknown table specifier {text.ToString()}";
                    return false;
                }

                if (specifiers.Contains(specifier))
                {
                    error = $"Duplicate table specifier {text.ToString()}";
                    return false;
                }

                specifiers.Add(specifier);
                continue;
            }

            if (item.SeparatorBefore == ':')
            {
                if (columns.Count == 0)
                {
                    error = "A column range must be between two columns";
                    return false;
                }

                rangeCount++;
            }

            columns.Add(UnescapeColumnName(text));
        }

        if (rangeCount > 1 || columns.Count > 2 || (columns.Count == 2 && rangeCount == 0))
        {
            error = "A structured reference can only refer to one column or one range of columns";
            return false;
        }

        if (!IsValidSpecifierCombination(specifiers))
        {
            error = "Invalid combination of table specifiers";
            return false;
        }

        descriptor = new StructuredReferenceDescriptor(
            tableName,
            SortSpecifiers(specifiers),
            columns.Count > 0 ? columns[0] : null,
            columns.Count > 1 ? columns[1] : null);
        return true;
    }

    private static bool IsValidSpecifierCombination(List<TableItemSpecifier> specifiers)
    {
        if (specifiers.Count <= 1)
            return true;

        if (specifiers.Count > 2)
            return false;

        return specifiers.Contains(TableItemSpecifier.Data) &&
               (specifiers.Contains(TableItemSpecifier.Headers) || specifiers.Contains(TableItemSpecifier.Totals));
    }

    private static bool TryParseSpecifier(ReadOnlySpan<char> text, out TableItemSpecifier specifier)
    {
        specifier = TableItemSpecifier.All;
        foreach (var candidate in CanonicalOrder)
        {
            if (text.Equals(SpecifierText(candidate), StringComparison.OrdinalIgnoreCase))
            {
                specifier = candidate;
                return true;
            }
        }

        return false;
    }

    private static string SpecifierText(TableItemSpecifier specifier) => specifier switch
    {
        TableItemSpecifier.All => "#All",
        TableItemSpecifier.Data => "#Data",
        TableItemSpecifier.Headers => "#Headers",
        TableItemSpecifier.Totals => "#Totals",
        TableItemSpecifier.ThisRow => "#This Row",
        _ => throw new ArgumentOutOfRangeException(nameof(specifier))
    };

    private static TableItemSpecifier[] SortSpecifiers(IEnumerable<TableItemSpecifier> specifiers) =>
        specifiers.Distinct().OrderBy(s => Array.IndexOf(CanonicalOrder, s)).ToArray();

    /// <summary>
    /// Writes the canonical text of a structured reference, e.g. Sales[Amount], Sales[@Amount], Sales[#Headers]
    /// or Sales[[#Headers],[C1]:[C2]]. #This Row is always written as @.
    /// </summary>
    public static string Format(StructuredReferenceDescriptor descriptor)
    {
        var sb = new StringBuilder();
        sb.Append(descriptor.TableName);
        sb.Append('[');

        var items = SortSpecifiers(descriptor.Items);
        var start = descriptor.ColumnStart;
        var end = descriptor.ColumnEnd;

        if (items.Length == 1 && items[0] == TableItemSpecifier.ThisRow)
        {
            sb.Append('@');
            if (start != null && end != null)
                AppendColumnRange(sb, start, end);
            else if (start != null)
                sb.Append(EscapeColumnName(start));
        }
        else if (items.Length == 0)
        {
            if (start != null && end != null)
                AppendColumnRange(sb, start, end);
            else if (start != null)
                sb.Append(EscapeColumnName(start));
        }
        else if (items.Length == 1 && start == null)
        {
            sb.Append(SpecifierText(items[0]));
        }
        else
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append('[').Append(SpecifierText(items[i])).Append(']');
            }

            if (start != null)
            {
                sb.Append(',');
                if (end != null)
                    AppendColumnRange(sb, start, end);
                else
                    sb.Append('[').Append(EscapeColumnName(start)).Append(']');
            }
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static void AppendColumnRange(StringBuilder sb, string start, string end)
    {
        sb.Append('[').Append(EscapeColumnName(start)).Append("]:[").Append(EscapeColumnName(end)).Append(']');
    }

    /// <summary>
    /// Finds the outermost '[' in <paramref name="text"/> that has no matching ']', e.g. the bracket after
    /// Sales in "=SUM(Sales[Am". Brackets inside string literals and quoted sheet names are ignored and
    /// ' escapes the next character inside brackets.
    /// </summary>
    /// <returns>The index of the unclosed '[', or -1 if every bracket is closed.</returns>
    public static int FindUnclosedBracket(ReadOnlySpan<char> text) => FindUnclosedBracket(text, out _);

    /// <summary>
    /// Finds the outermost unclosed '[' as <see cref="FindUnclosedBracket(ReadOnlySpan{char})"/> does.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="depth">How many brackets are still open at the end of <paramref name="text"/>.</param>
    private static int FindUnclosedBracket(ReadOnlySpan<char> text, out int depth)
    {
        depth = 0;
        var openIndex = -1;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (depth == 0)
            {
                if (c == '"')
                {
                    // skip the string literal, where "" is an escaped quote
                    i++;
                    while (i < text.Length)
                    {
                        if (text[i] == '"' && (i + 1 >= text.Length || text[i + 1] != '"'))
                            break;
                        if (text[i] == '"')
                            i++;
                        i++;
                    }
                }
                else if (c == '\'')
                {
                    // skip the quoted sheet name, e.g. 'Data [West]'!A1, where '' is an escaped quote.
                    // An unterminated sheet name consumes the rest of the text, as it does in the lexer.
                    i++;
                    while (i < text.Length)
                    {
                        if (text[i] == '\'' && (i + 1 >= text.Length || text[i + 1] != '\''))
                            break;
                        if (text[i] == '\'')
                            i++;
                        i++;
                    }
                }
                else if (c == '[')
                {
                    depth = 1;
                    openIndex = i;
                }

                continue;
            }

            if (c == EscapeChar)
                i++;
            else if (c == '[')
                depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0)
                    openIndex = -1;
            }
        }

        return depth > 0 ? openIndex : -1;
    }

    /// <summary>
    /// Completes the item of a structured reference that is being typed with <paramref name="item"/>, closing it
    /// with a bracket. When the item is one of a list of items, e.g. the "[Am" of "Sales[[#Headers],[Am", the
    /// reference's outer bracket is closed too, unless the rest of <paramref name="formula"/> already closes it.
    /// </summary>
    /// <param name="formula">The whole formula text.</param>
    /// <param name="itemStart">Where the item being typed starts.</param>
    /// <param name="itemEnd">Where the item being typed ends, usually the caret.</param>
    /// <param name="item">The text of the item, escaped as it should be written e.g. "Amount" or "#Headers".</param>
    /// <param name="isSpecifier">Whether the item is a table specifier such as #Headers.</param>
    /// <param name="replaceEnd">
    /// The end of the text to replace, from <paramref name="itemStart"/>. It takes in a closing bracket that comes
    /// next, so that it isn't written twice.
    /// </param>
    /// <param name="caretOffset">
    /// Where the caret goes in the returned text. That is after the last bracket, except when a specifier is
    /// closed along with the outer bracket: then it goes before the outer bracket, ready for the items that
    /// usually follow a specifier, e.g. "Sales[[#Headers]|]".
    /// </param>
    /// <returns>The text that replaces the text from <paramref name="itemStart"/> to <paramref name="replaceEnd"/>.</returns>
    public static string CompleteItem(string formula, int itemStart, int itemEnd, string item, bool isSpecifier,
        out int replaceEnd, out int caretOffset)
    {
        itemStart = Math.Clamp(itemStart, 0, formula.Length);
        itemEnd = Math.Clamp(itemEnd, itemStart, formula.Length);

        var text = item + "]";
        replaceEnd = SkipIfNext(formula, itemEnd, ']');
        caretOffset = text.Length;

        var outerOpen = FindUnclosedBracket(formula.AsSpan(0, itemStart), out var depth);
        if (outerOpen < 0 || depth < 2)
            return text;

        // only close the outer bracket if nothing after the item closes it already
        var completed = string.Concat(formula.AsSpan(0, itemStart), text, formula.AsSpan(replaceEnd));
        if (FindUnclosedBracket(completed) != outerOpen)
            return text;

        text += "]";
        if (!isSpecifier)
            caretOffset = text.Length;
        return text;
    }

    private static int SkipIfNext(string text, int pos, char c) =>
        pos < text.Length && text[pos] == c ? pos + 1 : pos;

    /// <summary>
    /// Finds what is being typed inside the brackets of a structured reference that ends at the caret, for
    /// suggesting columns and specifiers. e.g. for "=SUM(Sales[[#Headers],[Am" the table is Sales and the
    /// prefix is "Am".
    /// </summary>
    /// <param name="textUpToCaret">The formula text up to the caret.</param>
    /// <param name="tableName">The table name before the outermost unclosed '[', or null if there is none.</param>
    /// <param name="prefix">The (unescaped) start of the item being typed, which may be empty.</param>
    /// <param name="prefixStart">The index in <paramref name="textUpToCaret"/> where the item being typed starts.</param>
    /// <returns>
    /// True if the caret is inside an item of an unclosed structured reference. False if every bracket is closed,
    /// or if the caret is between items, e.g. after the "]" of "Sales[[#Headers]".
    /// </returns>
    public static bool TryGetSuggestionContext(ReadOnlySpan<char> textUpToCaret, out string? tableName,
        out string prefix, out int prefixStart)
    {
        tableName = null;
        prefix = string.Empty;
        prefixStart = textUpToCaret.Length;

        var open = FindUnclosedBracket(textUpToCaret);
        if (open < 0)
            return false;

        var nameStart = open;
        while (nameStart > 0 && RangeText.IsValidNameChar(textUpToCaret[nameStart - 1]))
            nameStart--;
        if (nameStart < open)
            tableName = textUpToCaret[nameStart..open].ToString();

        // the last separator that isn't escaped starts the item being typed
        var lastSeparator = open;
        for (int i = open + 1; i < textUpToCaret.Length; i++)
        {
            var c = textUpToCaret[i];
            if (c == EscapeChar)
                i++;
            else if (c is '[' or ']' or ',' or ';' or ':' or '@')
                lastSeparator = i;
        }

        // after a closed item, such as "[#Headers]", there is nothing to suggest until the next separator
        if (textUpToCaret[lastSeparator] == ']')
            return false;

        prefixStart = SkipWhitespace(textUpToCaret, lastSeparator + 1);
        prefix = UnescapeColumnName(textUpToCaret[prefixStart..].TrimEnd());
        return true;
    }

    private static int SkipWhitespace(ReadOnlySpan<char> text, int pos)
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            pos++;
        return pos;
    }

    private static int IndexOfUnescaped(ReadOnlySpan<char> text, char c)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == EscapeChar)
                i++;
            else if (text[i] == c)
                return i;
        }

        return -1;
    }
}
