using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Core.Interpreter2;
using Lexer = BlazorDatasheet.Formula.Core.Interpreter.Lexing.Lexer;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class Parser
{
    private int _position;
    private Token[] _tokens = null!;
    private List<string> _errors = null!;
    private readonly List<Reference> _references = new();

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
        var leftExpression = ParsePrimaryExpression();

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
                if (Peek(1).Tag == Tag.ColonToken)
                    return ParseRangeExpression();
                return ParseIdentifierExpression();
            case Tag.LeftCurlyBracketToken:
                return ParseArrayConstant();
            case Tag.Number: // special case for integer expression
                var numToken = (NumberToken)Current;
                if (numToken.IsInteger && Peek(1).Tag == Tag.ColonToken)
                    return ParseRangeExpression();
                break;
        }

        return ParseLiteralExpression();
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
                currentRow.Add(ParseLiteralExpression());
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

    private Expression ParseRangeExpression()
    {
        bool isValidRef = true;
        var rangeParts = new List<Reference>();
        while (true)
        {
            var rangePartToken = NextToken();
            if (rangePartToken.Tag == Tag.Number)
            {
                var numToken = (NumberToken)rangePartToken;
                if (numToken.IsInteger && numToken.Value < RangeText2.MaxRows)
                {
                    rangeParts.Add(new RowReference((int)numToken.Value, false));
                }
                else
                    isValidRef = false;
            }
            else if (rangePartToken.Tag == Tag.IdentifierToken)
            {
                var idToken = (IdentifierToken)rangePartToken;
                var validParse = RangeText2.TryParseSingleReference(idToken.Value, out var parsedRef);
                if (validParse)
                    rangeParts.Add(parsedRef!);
                else
                    isValidRef = false;
            }

            if (Current.Tag == Tag.ColonToken)
            {
                // consume colon token
                NextToken();
            }
            else
            {
                break;
            }
        }

        // check if valid range ref
        if (!isValidRef || rangeParts.Count < 2)
        {
            Error($"Bad reference found");
            return new LiteralExpression(CellValue.Error(new FormulaError(ErrorType.Ref)));
        }

        if (rangeParts.Count == 2)
        {
            var rangeRef = new RangeReference(rangeParts[0], rangeParts[1]);
            _references.Add(rangeRef);
            return new ReferenceExpression(rangeRef);
        }

        var multiRef = new MultiReference(rangeParts.ToArray());
        _references.Add(multiRef);
        return new ReferenceExpression(multiRef);
    }

    private LiteralExpression ParseLiteralExpression()
    {
        switch (Current.Tag)
        {
            case Tag.StringToken:
                var strToken = (StringToken)NextToken();
                return new LiteralExpression(CellValue.Text(strToken.Value));
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

        // If it's a single cell ref then return the range expression for it.
        // We have already checked in ParsePrimaryExpression that the next token is not a colon and so it's not part of a range.
        var isCellRef = RangeText2.TryParseSingleCellReference(identifierToken.Value.AsSpan(), out var cellRef);
        if (isCellRef)
        {
            _references.Add(cellRef!);
            return new ReferenceExpression(cellRef!);
        }

        if (bool.TryParse(identifierToken.Value.ToLower(), out var parsedBool))
            return new LiteralExpression(CellValue.Logical(parsedBool));

        return new NameExpression(identifierToken);
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
        return new Token(tag, Current.PositionStart);
    }
}