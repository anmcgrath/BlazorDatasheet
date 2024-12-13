using System.Data;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using Lexer = BlazorDatasheet.Formula.Core.Interpreter.Lexing.Lexer;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class Parser
{
    private int _position;
    private Token[] _tokens = null!;
    private List<string> _errors = null!;
    private List<Reference> _references = new();

    public SyntaxTree Parse(Token[] tokens, List<string>? lexErrors = null)
    {
        _position = 0;
        _tokens = tokens;
        _errors = lexErrors ?? new List<string>();
        // Formula must start with equals token
        MatchToken(Tag.EqualsToken);
        var expression = ParseExpression();
        MatchToken(Tag.Eof);
        return new SyntaxTree(expression, _references, _errors);
    }

    public SyntaxTree Parse(string formulaString)
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex(formulaString);
        _references = new();
        return Parse(tokens, lexer.Errors);
    }

    public CellFormula FromString(string formulaString)
    {
        return new CellFormula(Parse(formulaString));
    }

    private Expression ParseExpression()
    {
        return ParseUnaryExpression();
    }

    private Expression ParseUnaryExpression()
    {
        if (IsPrefixUnaryOperator(Current))
            return new UnaryOperatorExpression(NextToken(), ParseExpression());

        return ParseBinaryExpression();
    }

    private bool IsPrefixUnaryOperator(Token token)
    {
        return token.Tag == Tag.PlusToken ||
               token.Tag == Tag.MinusToken;
    }

    private Expression ParseBinaryExpression(int parentPrecedence = 0)
    {
        var leftExpression = ParsePrimaryExpression();

        while (Current.Tag == Tag.PercentToken)
            leftExpression = new UnaryOperatorExpression(NextToken(), leftExpression, isPostFix: true);

        while (true)
        {
            var precedence = Current.Tag.GetBinaryOperatorPrecedence();
            // If not a binary operator or if the precedence is less than the parent, exit loop
            if (precedence == 0 || precedence <= parentPrecedence)
                return leftExpression;

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            leftExpression = new BinaryOperationExpression(leftExpression, operatorToken, right);
        }
    }

    private Expression ParsePrimaryExpression()
    {
        switch (Current.Tag)
        {
            case Tag.LeftParenthToken:
                return ParseParenthExpression();
            case Tag.IdentifierToken:
                return ParseIdentifierExpression();
            case Tag.LogicalToken:
                return ParseLogicalExpression();
            case Tag.LeftCurlyBracketToken:
                return ParseArrayConstant();
            case Tag.SingleQuotedStringToken:
                return ParseSheetReference();
        }

        return ParseLiteralExpression();
    }

    private Expression ParseLogicalExpression()
    {
        var token = (LogicalToken)NextToken();
        return new LiteralExpression(CellValue.Logical(token.Value));
    }

    /// <summary>
    /// Parses reference of the form A1, or A1:A2 or 3:4 or A:B.
    /// With possible fixed rows/columns.
    /// </summary>
    /// <returns></returns>
    private Expression ParseSheetReference()
    {
        string? sheetNameAddr1 = null;
        if (Peek(1).Tag == Tag.BangToken)
        {
            sheetNameAddr1 = GetSheetName(NextToken());
            MatchToken(Tag.BangToken);
        }

        var firstTokenInRef = NextToken();

        if (firstTokenInRef.Tag == Tag.Number)
        {
            var numToken = (NumberToken)firstTokenInRef;
            if (numToken.IsInteger)
                return ParseRowReference(new RowAddress((int)numToken.Value, false), sheetNameAddr1);
        }

        Error("Could not parse reference");
        return new LiteralExpression(CellValue.Error(ErrorType.Ref));
    }

    private bool IsCellAddressToken(Token token)
    {
        return (token.Tag == Tag.AddressToken && ((AddressToken)token).Address.Kind == AddressKind.CellAddress);
    }

    private Expression ParseCellRangeReference(CellAddress firstCellAddress, string? sheetNameAddr1)
    {
        var firstCellRef = new CellReference(firstCellAddress.RowAddress.RowIndex, firstCellAddress.ColAddress.ColIndex,
            firstCellAddress.ColAddress.IsFixed, firstCellAddress.RowAddress.IsFixed);
        firstCellRef.SheetName = sheetNameAddr1;

        var isSingleCell = Current.Tag != Tag.ColonToken && !IsCellAddressToken(Peek(1)) &&
                           Peek(2).Tag != Tag.BangToken; // sheet locator
        if (isSingleCell) // single cell only
        {
            firstCellRef.SheetName = sheetNameAddr1;
            _references.Add(firstCellRef);
            return new ReferenceExpression(firstCellRef);
        }

        // if the next after the token isn't a cell ref, return single cell

        var colon = NextToken();
        string? sheetNameAddr2 = null;

        if (Current.Tag == Tag.SingleQuotedStringToken ||
            Current.Tag == Tag.IdentifierToken && Peek(1).Tag == Tag.BangToken)
        {
            sheetNameAddr2 = GetSheetName(NextToken());
            var bang = MatchToken(Tag.BangToken);
        }

        // Just handle the case if we are cell to cell reference.
        // If not, then return a single cell to allow for range intersection.
        var secondAddrToken = Peek(0);
        if (secondAddrToken.Tag == Tag.IdentifierToken &&
            RangeText.TryParseSingleAddress(((IdentifierToken)secondAddrToken).Value, out var secondCellAddress))
        {
            string? resolvedSheetName = null;
            if (secondCellAddress!.Kind == AddressKind.CellAddress)
            {
                NextToken();
                var rangeRef = new RangeReference(firstCellAddress, secondCellAddress.ToAddressType<CellAddress>());

                if (sheetNameAddr1 != null)
                    resolvedSheetName = sheetNameAddr1;
                else if (sheetNameAddr2 != null)
                    resolvedSheetName = sheetNameAddr2;

                rangeRef.SheetName = resolvedSheetName;
                _references.Add(rangeRef);
                return new ReferenceExpression(rangeRef);
            }
        }

        // allows for range intersection with row or col.
        _references.Add(firstCellRef);
        return new ReferenceExpression(firstCellRef);
    }

    private Expression ParseRowReference(RowAddress firstAddress, string? firstSheetName)
    {
        var colon = MatchToken(Tag.ColonToken);
        var rowToken = NextToken();
        Reference? rowRef = null;

        if (rowToken.Tag == Tag.IdentifierToken)
        {
            var parsedAddress =
                RangeText.TryParseSingleAddress(((IdentifierToken)rowToken).Value.AsSpan(), out var address);
            if (parsedAddress && address!.Kind == AddressKind.RowAddress)
            {
                rowRef = new RangeReference(firstAddress, address.ToAddressType<RowAddress>());
            }
        }
        else if (rowToken.Tag == Tag.Number)
        {
            var rowNumberToken = (NumberToken)rowToken;
            if (rowNumberToken.IsInteger)
                rowRef = new RangeReference(firstAddress, new RowAddress((int)rowNumberToken.Value - 1, false));
        }

        if (rowRef == null)
        {
            Error("Could not parse row reference");
            return new LiteralExpression(CellValue.Error(ErrorType.Ref));
        }

        rowRef.SheetName = firstSheetName;
        _references.Add(rowRef);
        return new ReferenceExpression(rowRef);
    }

    private Expression ParseColumnAddressReference(ColAddress firstAddress, string? sheetNameAddr1)
    {
        var colon = MatchToken(Tag.ColonToken);
        var secondToken = NextToken();
        if (secondToken.Tag == Tag.IdentifierToken)
        {
            var isValidAddr =
                RangeText.TryParseSingleAddress(((IdentifierToken)secondToken).Value.AsSpan(), out var address);
            if (isValidAddr)
            {
                var colRef = new RangeReference(firstAddress, address!.ToAddressType<ColAddress>());
                _references.Add(colRef);
                return new ReferenceExpression(colRef);
            }
            else
            {
                Error("Could not parse column address reference");
                return new LiteralExpression(CellValue.Error(ErrorType.Ref));
            }
        }

        // if we can't parse as a col address e.g c:c then should be a named address
        var idName = firstAddress.IsFixed ? "$" + firstAddress.ColStr : firstAddress.ColStr;
        return new ReferenceExpression(new NamedReference(idName, RangeText.IsValidNameAddress(idName)));
    }

    private string? GetSheetName(Token token)
    {
        if (token.Tag == Tag.SingleQuotedStringToken)
            return ((SheetLocatorToken)token).Text;
        if (token.Tag == Tag.IdentifierToken)
            return ((IdentifierToken)token).Value;

        return null;
    }

    private Expression ParseArrayConstant()
    {
        // Consume left bracket
        var leftBracket = MatchToken(Tag.LeftCurlyBracketToken);

        // Collect array items
        var rows = new List<List<LiteralExpression>>();
        var currentRow = new List<LiteralExpression>();

        // If no args
        if (Current.Tag != Tag.RightCurlyBracketToken)
        {
            while (true)
            {
                var parsedExpression = ParseLiteralExpression();
                if (parsedExpression.Kind != NodeKind.Literal)
                    continue;

                currentRow.Add((LiteralExpression)parsedExpression);
                if (Current.Tag == Tag.CommaToken)
                {
                    NextToken();
                    continue;
                }

                if (Current.Tag == Tag.SemiColonToken)
                {
                    NextToken();
                    rows.Add(new List<LiteralExpression>());
                    rows.Last().AddRange(currentRow);
                    currentRow.Clear();
                    continue;
                }

                break;
            }
        }

        rows.Add(currentRow);

        var isValid = true;
        // check all row lengths are the same
        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i].Count != rows[i - 1].Count)
            {
                isValid = false;
                break;
            }
        }

        if (!isValid)
            Error("Row lengths are not equal");

        MatchToken(Tag.RightCurlyBracketToken);

        return new ArrayConstantExpression(rows);
    }

    private Expression ParseLiteralExpression()
    {
        switch (Current.Tag)
        {
            case Tag.StringToken:
                var strToken = (StringToken)NextToken();
                return new StringLiteralExpression(CellValue.Text(strToken.Value));
            case Tag.Number:
                var nToken = (NumberToken)NextToken();
                if (Peek(0).Tag == Tag.ColonToken && nToken.IsInteger)
                    return ParseRowReference(new RowAddress((int)nToken.Value - 1, false), null);
                return new LiteralExpression(CellValue.Number(nToken.Value));
            default:
                var token = NextToken();
                Error($"Unable to parse literal {token.Tag}");
                return new LiteralExpression(CellValue.Error(ErrorType.Na));
        }
    }

    private void Error(string errorMessage)
    {
        _errors.Add(errorMessage);
    }

    private Expression ParseIdentifierExpression()
    {
        if (Peek(1).Tag == Tag.LeftParenthToken)
            return ParseFunctionCallExpression();

        if (Peek(1).Tag == Tag.BangToken)
            return ParseSheetReference();

        var identifierToken = (IdentifierToken)NextToken();

        if (bool.TryParse(identifierToken.Value.ToLower(), out var parsedBool))
            return new LiteralExpression(CellValue.Logical(parsedBool));

        // will be either a cell address, col address, row address or named address (valid or invalid).
        var isAddress = RangeText.TryParseSingleAddress(identifierToken.Value.AsSpan(), out var address);
        if (!isAddress)
        {
            Error($"Could not parse identifier {identifierToken.Value}");
            return new LiteralExpression(CellValue.Error(ErrorType.Ref));
        }

        if (address!.Kind == AddressKind.RowAddress && Peek(0).Tag == Tag.ColonToken)
            return ParseRowReference((RowAddress)address, null);

        if (address!.Kind == AddressKind.ColAddress && Peek(0).Tag == Tag.ColonToken)
            return ParseColumnAddressReference((ColAddress)address, null);

        if (address!.Kind == AddressKind.CellAddress)
            return ParseCellRangeReference((CellAddress)address, null);

        if (address!.Kind == AddressKind.NamedAddress)
        {
            var namedAddress = (NamedAddress)address;
            var namedRef = new NamedReference(namedAddress.Name, namedAddress.IsValid);
            _references.Add(namedRef);
            return new ReferenceExpression(namedRef);
        }

        var idRef = new NamedReference(identifierToken.Value,
            RangeText.IsValidNameAddress(identifierToken.Value));
        _references.Add(idRef);
        return new ReferenceExpression(idRef);
    }

    private Expression ParseFunctionCallExpression()
    {
        var funcToken = (IdentifierToken)NextToken();
        var leftParenth = MatchToken(Tag.LeftParenthToken);
        var args = new List<Expression>();

        while (Current.Tag != Tag.RightParenthToken)
        {
            var arg = ParseExpression();
            args.Add(arg);
            if (Current.Tag != Tag.CommaToken)
                break;
            else
                NextToken();
        }

        MatchToken(Tag.RightParenthToken);
        return new FunctionExpression(funcToken, args);
    }

    private Expression ParseParenthExpression()
    {
        var left = NextToken();
        var expr = ParseExpression();
        var right = MatchToken(Tag.RightParenthToken);
        return new ParenthesizedExpression(left, expr, right);
    }

    /// <summary>
    /// Look at (but don't consume) the token at the offset specified
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    private Token Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length)
            return _tokens[^1];

        return _tokens[index];
    }

    private Token Current => Peek(0);

    /// <summary>
    /// Consumes and returns the next token
    /// </summary>
    /// <returns></returns>
    private Token NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    /// <summary>
    ///     Return the current token if it matches the token kind provided and moves to the next token.
    ///     Otherwise flag an error and return a token of the kind provided
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    private Token MatchToken(Tag tag)
    {
        if (Current.Tag == tag)
            return NextToken();

        _errors.Add($"ERROR: Unexpected token: <{Current.Tag}>. Expected {tag}");
        return new Token(tag, "", Current.PositionStart);
    }
}