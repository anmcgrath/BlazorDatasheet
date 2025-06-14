using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using Lexer = BlazorDatasheet.Formula.Core.Interpreter.Lexing.Lexer;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class Parser
{
    private readonly IEnvironment _environment;
    private int _position;
    private List<Token> _tokens = null!;
    private List<string> _errors = null!;
    private List<Reference> _references = new();
    private bool _containsVolatiles;
    private ParsingContext? _parsingContext;
    private FormulaOptions _formulaOptions;

    public Parser(IEnvironment environment, FormulaOptions? formulaOptions = null)
    {
        _formulaOptions = formulaOptions ?? new FormulaOptions();
        _environment = environment;
    }

    public SyntaxTree Parse(List<Token> tokens, List<string>? lexErrors = null)
    {
        _position = 0;
        _tokens = tokens;
        _errors = lexErrors ?? new List<string>();
        _containsVolatiles = false;
        // Formula must start with equals token
        MatchToken(Tag.EqualsToken);
        var expression = ParseExpression();
        MatchToken(Tag.Eof);

        foreach (var reference in _references)
        {
            if (!reference.ExplicitSheetName && _parsingContext != null)
                reference.SetSheetName(_parsingContext.CallingSheetName, _parsingContext.ExplicitSheetNameReference);
        }

        return new SyntaxTree(expression, _references, _errors);
    }

    public SyntaxTree Parse(string formulaString, ParsingContext? parsingContext = null)
    {
        _parsingContext = parsingContext;
        var lexer = new Lexer();
        var tokens = lexer.Lex(formulaString, _formulaOptions);
        _references = new();
        return Parse(tokens, lexer.Errors);
    }

    public CellFormula FromString(string formulaString, ParsingContext? parsingContext = null)
    {
        _parsingContext = parsingContext ?? _parsingContext;
        return new CellFormula(Parse(formulaString, parsingContext), _containsVolatiles);
    }

    private Expression ParseExpression()
    {
        return ParseUnaryExpression();
    }

    private Expression ParseUnaryExpression()
    {
        if (IsUnaryOperator(Current))
        {
            return new UnaryOperatorExpression(NextToken(), ParseExpression());
        }

        return ParseBinaryExpression();
    }

    private bool IsUnaryOperator(Token token)
    {
        return token.Tag == Tag.PlusToken ||
               token.Tag == Tag.MinusToken;
    }

    private Expression ParseBinaryExpression(int parentPrecedence = 0)
    {
        var leftExpression = IsUnaryOperator(Current) ? ParseUnaryExpression() : ParsePrimaryExpression();

        while (Current.Tag == Tag.PercentToken)
            leftExpression = new UnaryOperatorExpression(NextToken(), leftExpression, isPostFix: true);

        while (true)
        {
            var precedence = Current.Tag.GetBinaryOperatorPrecedence();
            // If not a binary operator or if the precedence is less than the parent, exit loop
            if (precedence == 0 || precedence <= parentPrecedence)
                return leftExpression;

            if (Current.Tag.IsVolatileBinaryExpression())
                _containsVolatiles = true;

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
            case Tag.SheetLocatorToken:
                return ParseSheetReferenceExpression();
            case Tag.AddressToken:
                return ParseReferenceExpressionFromAddress(((AddressToken)NextToken()).Address);
            case Tag.ErrorToken:
                return new ErrorExpression(((ErrorToken)NextToken()).ErrorType);
        }

        if (Peek(1).Tag == Tag.ColonToken && TryConvertToAddress(Current, out var address))
        {
            NextToken();
            return ParseReferenceExpressionFromAddress(address!);
        }

        return ParseLiteralExpression();
    }

    private bool TryConvertToAddress(Token token, out Address? addressToken)
    {
        addressToken = null;

        if (token.Tag == Tag.AddressToken)
        {
            addressToken = ((AddressToken)token).Address;
            return true;
        }

        if (token.Tag == Tag.Number)
        {
            var numberToken = (NumberToken)token;
            if (numberToken.IsInteger)
            {
                addressToken = new RowAddress((int)numberToken.Value - 1, false);
                return true;
            }
        }

        if (token.Tag == Tag.IdentifierToken && ((IdentifierToken)token).Value.All(char.IsLetter))
        {
            var colIndex = RangeText.ColStrToIndex(((IdentifierToken)token).Value);
            if (colIndex < RangeText.MaxCols)
            {
                addressToken = new ColAddress(colIndex, false);
                return true;
            }
        }

        return false;
    }

    private Expression ParseSheetReferenceExpression()
    {
        var sheetToken = (SheetLocatorToken)NextToken();

        if (!TryConvertToAddress(NextToken(), out var address))
        {
            Error("Expected address token after sheet locator");
            return new LiteralExpression(CellValue.Error(ErrorType.Ref));
        }

        var parsedRef = ParseReferenceExpressionFromAddress(address!, sheetToken.Text);
        if (parsedRef is not ReferenceExpression)
        {
            Error($"Unexpected expression {parsedRef}");
            return new LiteralExpression(CellValue.Error(ErrorType.Ref));
        }

        ((ReferenceExpression)parsedRef).Reference.SetSheetName(sheetToken.Text);
        return parsedRef;
    }

    private Expression ParseReferenceExpressionFromAddress(Address address, string? sheetName = null)
    {
        if (Peek(0).Tag != Tag.ColonToken)
        {
            var firstRef = GetReferenceFromAddress(address);
            if (firstRef == null)
                return new LiteralExpression(CellValue.Error(ErrorType.Ref));

            _references.Add(firstRef);
            return new ReferenceExpression(firstRef);
        }

        MatchToken(Tag.ColonToken);
        var firstSheetName = sheetName ?? null;

        if (Peek(0).Tag == Tag.SheetLocatorToken)
        {
            var sheetToken = NextToken();
            if (sheetToken.Text != firstSheetName)
                return ErrorAndReturnLiteral("References must be on the same sheet", ErrorType.Ref);
        }

        if (TryConvertToAddress(NextToken(), out var nextAddr))
        {
            if (nextAddr == null)
                return ErrorAndReturnLiteral("Invalid reference", ErrorType.Ref);

            if (nextAddr.Kind != address.Kind)
                return ErrorAndReturnLiteral($"Invalid reference {address.Kind} to {nextAddr.Kind}",
                    ErrorType.Ref);

            ReferenceExpression? evalRef = address.Kind switch
            {
                AddressKind.CellAddress => new ReferenceExpression(new RangeReference((CellAddress)address,
                    (CellAddress)nextAddr)),
                AddressKind.RowAddress => new ReferenceExpression(new RangeReference((RowAddress)address,
                    (RowAddress)nextAddr)),
                AddressKind.ColAddress => new ReferenceExpression(new RangeReference((ColAddress)address,
                    (ColAddress)nextAddr)),
                _ => null
            };

            if (evalRef == null) return ErrorAndReturnLiteral("Invalid reference", ErrorType.Ref);

            _references.Add(evalRef.Reference);
            return evalRef;
        }

        return ErrorAndReturnLiteral("Invalid reference", ErrorType.Ref);
    }

    private Expression ErrorAndReturnLiteral(string message, ErrorType errorType)
    {
        Error(message);
        return new LiteralExpression(CellValue.Error(errorType));
    }

    private Expression ParseLogicalExpression()
    {
        var token = (LogicalToken)NextToken();
        return new LiteralExpression(CellValue.Logical(token.Value));
    }


    private Reference? GetReferenceFromAddress(Address address)
    {
        switch (address.Kind)
        {
            case AddressKind.CellAddress:
                var cellAddress = (CellAddress)address;
                return new CellReference(cellAddress.RowAddress.RowIndex, cellAddress.ColAddress.ColIndex,
                    cellAddress.ColAddress.IsFixed, cellAddress.RowAddress.IsFixed);
            case AddressKind.NamedAddress:
                var namedAddress = (NamedAddress)address;
                return new NamedReference(namedAddress.Name);
        }

        return null;
    }

    private Expression ParseArrayConstant()
    {
        // Consume left bracket
        MatchToken(Tag.LeftCurlyBracketToken);

        // Collect array items
        var rows = new List<List<LiteralExpression>>();
        var currentRow = new List<LiteralExpression>();

        // If no args
        if (Current.Tag != Tag.RightCurlyBracketToken)
        {
            while (true)
            {
                currentRow.Add(ParseLiteralExpression());
                if (Current.Tag == _formulaOptions.SeparatorSettings.ColumnSeparatorTag)
                {
                    NextToken();
                    continue;
                }

                if (Current.Tag == _formulaOptions.SeparatorSettings.RowSeparatorTag)
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

        return new ArrayConstantExpression(rows, _formulaOptions);
    }

    private LiteralExpression ParseLiteralExpression()
    {
        switch (Current.Tag)
        {
            case Tag.StringToken:
                var strToken = (StringToken)NextToken();
                return new StringLiteralExpression(CellValue.Text(strToken.Value));
            case Tag.Number:
                var nToken = (NumberToken)NextToken();
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

        var identifierToken = (IdentifierToken)NextToken();

        if (Current.Tag == Tag.ColonToken && TryConvertToAddress(identifierToken, out var address))
            return ParseReferenceExpressionFromAddress(address!);

        _references.Add(new NamedReference(identifierToken.Value));
        return new VariableExpression(identifierToken);
    }

    private Expression ParseFunctionCallExpression()
    {
        var funcToken = (IdentifierToken)NextToken();
        MatchToken(Tag.LeftParenthToken);
        var args = new List<Expression>();
        var argIndices = new List<int>();

        while (Current.Tag != Tag.RightParenthToken)
        {
            argIndices.Add(Current.PositionStart);

            var arg = ParseExpression();
            args.Add(arg);
            if (Current.Tag != _formulaOptions.SeparatorSettings.FuncParameterSeparatorTag)
                break;
            else
                NextToken();
        }

        var rightParenth = MatchToken(Tag.RightParenthToken);

        if (!_environment.FunctionExists(funcToken.Value))
            Error($"Function {funcToken.Value} does not exist");

        var functionDefinition = _environment.GetFunctionDefinition(funcToken.Value);
        if (functionDefinition != null && functionDefinition.IsVolatile)
            _containsVolatiles = true;

        return new FunctionExpression(funcToken, args, functionDefinition,
            _formulaOptions, argIndices.ToArray(), rightParenth);
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
        if (index >= _tokens.Count)
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