using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core;

public enum FormulaReferenceSpanKind
{
    Cell,
    Range,
    Named
}

/// <summary>
/// A reference found in formula text, along with the exact text it occupies.
/// </summary>
/// <param name="Index">The position of this reference amongst the references in the text.</param>
/// <param name="TextStart">The index of the first character of the reference, including any sheet name.</param>
/// <param name="TextLength">The number of characters the reference occupies.</param>
/// <param name="Kind"></param>
/// <param name="SheetName">The sheet name, if one was given explicitly.</param>
/// <param name="Name">The name of the reference, if it is a named reference.</param>
/// <param name="Region">The region referred to. Null for named references, which must be resolved by the caller.</param>
/// <param name="IsStartColFixed"></param>
/// <param name="IsStartRowFixed"></param>
/// <param name="IsEndColFixed"></param>
/// <param name="IsEndRowFixed"></param>
/// <param name="ColorIndex">A value from 1 to <see cref="FormulaReferenceScanner.ColorCount"/>.</param>
public sealed record FormulaReferenceSpan(
    int Index,
    int TextStart,
    int TextLength,
    FormulaReferenceSpanKind Kind,
    string? SheetName,
    string? Name,
    IRegion? Region,
    bool IsStartColFixed,
    bool IsStartRowFixed,
    bool IsEndColFixed,
    bool IsEndRowFixed,
    int ColorIndex)
{
    public int TextEnd => TextStart + TextLength;
}

/// <summary>
/// Finds the references in formula text. Works on tokens rather than a parsed formula, so that it
/// gives sensible results for the incomplete formulas that exist while a user is typing.
/// </summary>
public static class FormulaReferenceScanner
{
    /// <summary>
    /// The number of distinct colours that references cycle through.
    /// </summary>
    public const int ColorCount = 5;

    /// <summary>
    /// Returns the references in <paramref name="text"/>, in the order they appear.
    /// </summary>
    /// <param name="text">The formula text, including the leading equals sign.</param>
    /// <param name="options"></param>
    /// <param name="isNamedReference">Identifies the names that are references. If null, names are ignored.</param>
    public static IReadOnlyList<FormulaReferenceSpan> Scan(ReadOnlySpan<char> text, FormulaOptions options,
        Func<string, bool>? isNamedReference = null)
    {
        var spans = new List<FormulaReferenceSpan>();
        if (text.IsEmpty || text[0] != '=')
            return spans;

        // Whitespace is kept as tokens so that the end of a reference is exact.
        var allTokens = new Lexer().Lex(text, options, WhiteSpaceOptions.PreserveAll);
        var tokens = new List<Token>(allTokens.Count);
        var tokenEnds = new List<int>(allTokens.Count);
        for (var i = 0; i < allTokens.Count - 1; i++)
        {
            if (allTokens[i].Tag == Tag.Whitespace)
                continue;
            tokens.Add(allTokens[i]);
            tokenEnds.Add(allTokens[i + 1].PositionStart);
        }

        for (var i = 0; i < tokens.Count; i++)
        {
            var start = tokens[i].PositionStart;
            var colorIndex = (spans.Count % ColorCount) + 1;

            if (TryReadReference(tokens, i, out var tokenCount, out var read))
            {
                var end = tokenEnds[i + tokenCount - 1];
                spans.Add(new FormulaReferenceSpan(spans.Count, start, end - start, read.Kind, read.SheetName, null,
                    read.Region, read.IsStartColFixed, read.IsStartRowFixed, read.IsEndColFixed, read.IsEndRowFixed,
                    colorIndex));
                i += tokenCount - 1;
                continue;
            }

            if (isNamedReference != null &&
                tokens[i] is IdentifierToken identifier &&
                !IsFollowedBy(tokens, i, Tag.LeftParenthToken) &&
                isNamedReference(identifier.Value))
            {
                spans.Add(new FormulaReferenceSpan(spans.Count, start, tokenEnds[i] - start,
                    FormulaReferenceSpanKind.Named, null, identifier.Value, null, false, false, false, false,
                    colorIndex));
            }
        }

        return spans;
    }

    private readonly record struct ReadReference(
        FormulaReferenceSpanKind Kind,
        string? SheetName,
        IRegion Region,
        bool IsStartColFixed,
        bool IsStartRowFixed,
        bool IsEndColFixed,
        bool IsEndRowFixed);

    private static bool IsFollowedBy(List<Token> tokens, int index, Tag tag) =>
        index + 1 < tokens.Count && tokens[index + 1].Tag == tag;

    private static bool TryReadReference(List<Token> tokens, int startIndex, out int tokenCount,
        out ReadReference reference)
    {
        tokenCount = 0;
        reference = default;
        var index = startIndex;

        string? sheetName = null;
        if (tokens[index].Tag == Tag.SheetLocatorToken)
        {
            sheetName = tokens[index].Text;
            index++;
        }

        var allowImplicitRowOrCol = sheetName != null || IsFollowedBy(tokens, index, Tag.ColonToken);
        if (index >= tokens.Count ||
            !TryConvertToAddress(tokens[index], allowImplicitRowOrCol, out var first))
            return false;

        // something like LOG10( is a function, even though LOG10 is a valid cell address
        if (sheetName == null && IsFollowedBy(tokens, index, Tag.LeftParenthToken))
            return false;

        index++;

        Address? second = null;
        if (index < tokens.Count && tokens[index].Tag == Tag.ColonToken)
        {
            if (TryReadRangeEnd(tokens, index + 1, sheetName, first.Kind, out var endIndex, out second))
                index = endIndex;
            else if (first.Kind != AddressKind.CellAddress)
                return false;
            // otherwise the range is still being typed (e.g. A1:), so the cell is the reference for now.
        }

        tokenCount = index - startIndex;
        reference = ToReference(sheetName, first, second);
        return true;
    }

    /// <summary>
    /// Reads the part of a range after the colon. <paramref name="endIndex"/> is the index after the last token read.
    /// </summary>
    private static bool TryReadRangeEnd(List<Token> tokens, int index, string? sheetName, AddressKind kind,
        out int endIndex, out Address? address)
    {
        endIndex = index;
        address = null;

        if (index < tokens.Count && tokens[index].Tag == Tag.SheetLocatorToken)
        {
            if (sheetName == null || tokens[index].Text != sheetName)
                return false;

            index++;
        }

        if (index >= tokens.Count ||
            !TryConvertToAddress(tokens[index], true, out address) ||
            address.Kind != kind)
        {
            return false;
        }

        endIndex = index + 1;
        return true;
    }

    private static ReadReference ToReference(string? sheetName, Address first, Address? second)
    {
        var kind = second == null && first.Kind == AddressKind.CellAddress
            ? FormulaReferenceSpanKind.Cell
            : FormulaReferenceSpanKind.Range;

        second ??= first;

        switch (first)
        {
            case ColAddress startCol:
            {
                var endCol = (ColAddress)second;
                return new ReadReference(kind, sheetName,
                    new ColumnRegion(Math.Min(startCol.ColIndex, endCol.ColIndex),
                        Math.Max(startCol.ColIndex, endCol.ColIndex)),
                    startCol.IsFixed, false, endCol.IsFixed, false);
            }
            case RowAddress startRow:
            {
                var endRow = (RowAddress)second;
                return new ReadReference(kind, sheetName,
                    new RowRegion(Math.Min(startRow.RowIndex, endRow.RowIndex),
                        Math.Max(startRow.RowIndex, endRow.RowIndex)),
                    false, startRow.IsFixed, false, endRow.IsFixed);
            }
            default:
            {
                var startCell = (CellAddress)first;
                var endCell = (CellAddress)second;
                return new ReadReference(kind, sheetName,
                    new Region(startCell.RowAddress.RowIndex, endCell.RowAddress.RowIndex,
                        startCell.ColAddress.ColIndex, endCell.ColAddress.ColIndex),
                    startCell.ColAddress.IsFixed, startCell.RowAddress.IsFixed,
                    endCell.ColAddress.IsFixed, endCell.RowAddress.IsFixed);
            }
        }
    }

    private static bool TryConvertToAddress(Token token, bool allowImplicitRowOrCol, out Address address)
    {
        address = null!;

        if (token is AddressToken addressToken &&
            addressToken.Address.Kind is AddressKind.CellAddress or AddressKind.RowAddress or AddressKind.ColAddress)
        {
            address = addressToken.Address;
            return true;
        }

        if (allowImplicitRowOrCol &&
            token is NumberToken numberToken &&
            numberToken.IsInteger &&
            numberToken.Value is >= 1 and <= RangeText.MaxRows)
        {
            address = new RowAddress((int)numberToken.Value - 1, false);
            return true;
        }

        if (allowImplicitRowOrCol &&
            token is IdentifierToken identifierToken &&
            identifierToken.Value.All(char.IsLetter))
        {
            var colIndex = RangeText.ColStrToIndex(identifierToken.Value);
            if (colIndex < RangeText.MaxCols)
            {
                address = new ColAddress(colIndex, false);
                return true;
            }
        }

        return false;
    }
}
