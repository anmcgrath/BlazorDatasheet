using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Edit.DefaultComponents;

internal static class FormulaReferenceColorizer
{
    public static Dictionary<int, int> GetReferenceColorIndices(IReadOnlyList<Token> tokens)
    {
        var tokenColors = new Dictionary<int, int>();
        var referenceCount = 0;

        for (var i = 0; i < tokens.Count - 1; i++)
        {
            if (!TryReadReference(tokens, i, out var tokenCount))
                continue;

            var colorIndex = (referenceCount % 5) + 1;
            for (var j = i; j < i + tokenCount; j++)
                tokenColors[j] = colorIndex;

            referenceCount++;
            i += tokenCount - 1;
        }

        return tokenColors;
    }

    private static bool TryReadReference(IReadOnlyList<Token> tokens, int startIndex, out int tokenCount)
    {
        tokenCount = 0;
        var index = startIndex;

        string? sheetName = null;
        if (tokens[index].Tag == Tag.SheetLocatorToken)
        {
            sheetName = tokens[index].Text;
            index++;
        }

        var allowImplicitRowOrCol = sheetName != null ||
                                    (index + 1 < tokens.Count - 1 && tokens[index + 1].Tag == Tag.ColonToken);
        if (index >= tokens.Count - 1 ||
            !TryConvertToAddressKind(tokens[index], allowImplicitRowOrCol, out var firstAddressKind))
            return false;

        index++;

        if (index < tokens.Count - 1 && tokens[index].Tag == Tag.ColonToken)
        {
            index++;

            if (index < tokens.Count - 1 && tokens[index].Tag == Tag.SheetLocatorToken)
            {
                if (sheetName == null || tokens[index].Text != sheetName)
                    return false;

                index++;
            }

            if (index >= tokens.Count - 1 ||
                !TryConvertToAddressKind(tokens[index], true, out var secondAddressKind) ||
                secondAddressKind != firstAddressKind)
            {
                return false;
            }

            index++;
        }

        tokenCount = index - startIndex;
        return true;
    }

    private static bool TryConvertToAddressKind(Token token, bool allowImplicitRowOrCol, out AddressKind addressKind)
    {
        addressKind = default;

        if (token is AddressToken addressToken &&
            addressToken.Address.Kind is AddressKind.CellAddress or AddressKind.RowAddress or AddressKind.ColAddress)
        {
            addressKind = addressToken.Address.Kind;
            return true;
        }

        if (allowImplicitRowOrCol &&
            token is NumberToken numberToken &&
            numberToken.IsInteger &&
            numberToken.Value is >= 1 and <= RangeText.MaxRows)
        {
            addressKind = AddressKind.RowAddress;
            return true;
        }

        if (allowImplicitRowOrCol &&
            token is IdentifierToken identifierToken &&
            identifierToken.Value.All(char.IsLetter))
        {
            var colIndex = RangeText.ColStrToIndex(identifierToken.Value);
            if (colIndex < RangeText.MaxCols)
            {
                addressKind = AddressKind.ColAddress;
                return true;
            }
        }

        return false;
    }
}
