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
            case Tag.LeftCurlyBracketToken:
                return ParseArrayConstant();
            case Tag.AddressToken:
            case Tag.SingleQuotedStringToken:
                return ParseReference();
        }

        return ParseLiteralExpression();
    }

    /// <summary>
    /// Parses reference of the form A1, or A1:A2 or 3:4 or A:B.
    /// With possible fixed rows/columns.
    /// </summary>
    /// <returns></returns>
    private Expression ParseReference()
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

        if (firstTokenInRef.Tag == Tag.AddressToken)
        {
            var addrToken = (AddressToken)firstTokenInRef;
            if (addrToken.Address.Kind == AddressKind.RowAddress)
                return ParseRowReference((RowAddress)addrToken.Address, sheetNameAddr1);
            else if (addrToken.Address.Kind == AddressKind.CellAddress)
                return ParseCellRangeReference((CellAddress)addrToken.Address, sheetNameAddr1);
            else if (addrToken.Address.Kind == AddressKind.ColAddress)
                return ParseColumnAddressReference((ColAddress)addrToken.Address, sheetNameAddr1);
        }

        Error("Could not parse reference");
        return new LiteralExpression(CellValue.Error(ErrorType.Ref));
    }

    private bool IsCellAddressToken(Token token)
    {
        return token.Tag == Tag.AddressToken && ((AddressToken)token).Address.Kind == AddressKind.CellAddress;
    }

    private Expression ParseCellRangeReference(CellAddress firstCellAddress, string? sheetNameAddr1)
    {
        var isSingleCell =Current.Tag != Tag.ColonToken || !IsCellAddressToken(Peek(1));
        if (isSingleCell) // single cell only
        {
            var cellRef = new CellReference(firstCellAddress.RowAddress.RowIndex, firstCellAddress.ColAddress.ColIndex,
                firstCellAddress.ColAddress.IsFixed, firstCellAddress.RowAddress.IsFixed);
            cellRef.SheetName = sheetNameAddr1;
            return new ReferenceExpression(cellRef);
        }

        // if the next after the token isn't a cell ref, return single cell

        var colon = NextToken();
        string? sheetNameAddr2 = null;

        if (Current.Tag == Tag.SingleQuotedStringToken ||
            Current.Tag == Tag.IdentifierToken && Peek(1).Tag == Tag.BangToken)
        {
            sheetNameAddr2 = GetSheetName(NextToken());
        }

        var secondAddrToken = NextToken();
        if (secondAddrToken.Tag == Tag.AddressToken)
        {
            var addrToken = (AddressToken)secondAddrToken;
            if (addrToken.Address.Kind == AddressKind.CellAddress)
            {
                var cellAddress = (CellAddress)addrToken.Address;
                var rangeRef = new RangeReference(firstCellAddress, cellAddress);
                string? resolvedSheetName = null;
                if (sheetNameAddr1 != null && sheetNameAddr2 != null)
                {
                    if (sheetNameAddr1 == sheetNameAddr2)
                    {
                        resolvedSheetName = sheetNameAddr1;
                    }
                    else
                    {
                        Error("Sheet names for cell references must match");
                    }
                }

                if (sheetNameAddr1 != null)
                    resolvedSheetName = sheetNameAddr1;
                else if (sheetNameAddr2 != null)
                    resolvedSheetName = sheetNameAddr2;

                rangeRef.SheetName = resolvedSheetName;
                return new ReferenceExpression(rangeRef);
            }
        }

        Error("Could not parse cell range references");
        return new LiteralExpression(CellValue.Error(ErrorType.Ref));
    }

    private Expression ParseRowReference(RowAddress firstAddress, string? firstSheetName)
    {
        var colon = MatchToken(Tag.ColonToken);
        var rowToken = NextToken();
        Reference? rowRef = null;
        if (rowToken.Tag == Tag.AddressToken && ((AddressToken)rowToken).Address.Kind == AddressKind.RowAddress)
            rowRef = new RangeReference(firstAddress, (RowAddress)((AddressToken)rowToken).Address);
        else if (rowToken.Tag == Tag.Number)
        {
            var rowNumberToken = (NumberToken)rowToken;
            if (rowNumberToken.IsInteger)
                rowRef = new RangeReference(firstAddress, new RowAddress((int)rowNumberToken.Value, false));
        }

        if (rowRef == null)
        {
            Error("Could not parse row reference");
            return new LiteralExpression(CellValue.Error(ErrorType.Ref));
        }

        rowRef.SheetName = firstSheetName;
        return new ReferenceExpression(rowRef);
    }

    private Expression ParseColumnAddressReference(ColAddress firstAddress, string? sheetNameAddr1)
    {
        var colon = MatchToken(Tag.ColonToken);
        var secondToken = NextToken();
        if (secondToken.Tag == Tag.AddressToken)
        {
            var addressToken = (AddressToken)secondToken;
            if (addressToken.Address.Kind == AddressKind.ColAddress)
            {
                var reference = new RangeReference(firstAddress, (ColAddress)addressToken.Address);
                reference.SheetName = sheetNameAddr1;
                return new ReferenceExpression(reference);
            }
        }

        // if we can't parse as a col address e.g c:c then should be a named address
        var idName = firstAddress.IsFixed ? "$" + firstAddress.ColStr : firstAddress.ColStr;
        return new ReferenceExpression(new NamedReference(idName, RangeText.IsValidNameAddress(idName)));
    }

    private string? GetSheetName(Token token)
    {
        if (token.Tag == Tag.SingleQuotedStringToken)
            return ((QuotedSheetNameToken)token).Text;
        if (token.Tag == Tag.IdentifierToken)
            return ((IdentifierToken)token).Value;

        return null;
    }

    private Reference? GetReferenceFromAddress(Address address)
    {
        switch (address.Kind)
        {
            case AddressKind.CellAddress:
                var cellAddress = (CellAddress)address;
                return new CellReference(cellAddress.RowAddress.RowIndex, cellAddress.ColAddress.ColIndex,
                    cellAddress.ColAddress.IsFixed, cellAddress.RowAddress.IsFixed);
            case AddressKind.RangeAddress:
                var rangeAddress = (RangeAddress)address;
                if (rangeAddress.Start.Kind == AddressKind.CellAddress &&
                    rangeAddress.End.Kind == AddressKind.CellAddress)
                    return new RangeReference((CellAddress)rangeAddress.Start, (CellAddress)rangeAddress.End);
                if (rangeAddress.Start.Kind == AddressKind.ColAddress &&
                    rangeAddress.End.Kind == AddressKind.ColAddress)
                    return new RangeReference((ColAddress)rangeAddress.Start, (ColAddress)rangeAddress.End);
                if (rangeAddress.Start.Kind == AddressKind.RowAddress &&
                    rangeAddress.End.Kind == AddressKind.RowAddress)
                    return new RangeReference((RowAddress)rangeAddress.Start, (RowAddress)rangeAddress.End);
                Error("Invalid range address");
                return null;
            case AddressKind.NamedAddress:
                var namedAddress = (NamedAddress)address;
                if (!namedAddress.IsValid)
                {
                    Error($"Invalid named address {namedAddress.Name}");
                }

                return new NamedReference(namedAddress.Name, namedAddress.IsValid);
        }

        throw new Exception($"Unhandled address type {address.Kind}");
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
                if (Peek(1).Tag == Tag.ColonToken)
                    return ParseReference();
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
        {
            var id = ((IdentifierToken)NextToken()).Value;
            var bang = MatchToken(Tag.BangToken); // bang token
            return ParseReference();
        }

        var identifierToken = (IdentifierToken)NextToken();

        if (bool.TryParse(identifierToken.Value.ToLower(), out var parsedBool))
            return new LiteralExpression(CellValue.Logical(parsedBool));

        var isValidName = RangeText.IsValidNameAddress(identifierToken.Value.AsSpan());
        if (!isValidName)
            Error($"Invalid identifier {identifierToken.Value}");

        var namedRef = new NamedReference(identifierToken.Value, isValidName);
        if (isValidName)
        {
            _references.Add(namedRef);
        }

        return new ReferenceExpression(namedRef);
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