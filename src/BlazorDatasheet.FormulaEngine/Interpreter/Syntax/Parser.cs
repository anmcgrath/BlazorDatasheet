using BlazorDatasheet.FormulaEngine.Interpreter.References;
using ExpressionEvaluator.CodeAnalysis.Types;

namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

internal class Parser
{
    private SyntaxToken[] _tokens;
    private int _position;

    public Parser()
    {
    }

    public List<string> Errors;

    /// <summary>
    /// Keep track of cell & variable references
    /// </summary>
    private List<Reference> _references;

    private SyntaxToken Current => Peek(0);

    public SyntaxTree Parse(Lexer lexer, string text)
    {
        Errors = new List<string>();
        _references = new List<Reference>();
        _position = 0;

        SyntaxToken token;
        var tokens = new List<SyntaxToken>();
        lexer.Begin(text);
        do
        {
            token = lexer.Lex();
            if (token.Kind != SyntaxKind.WhitespaceToken &&
                token.Kind != SyntaxKind.BadToken)
                tokens.Add(token);
        } while (token.Kind != SyntaxKind.EndOfFileToken);

        _tokens = tokens.ToArray();
        Errors.AddRange(lexer.Errors);

        // Formula must start with an equals token
        MatchToken(SyntaxKind.EqualsToken);
        var expression = ParseExpression();
        var eof = MatchToken(SyntaxKind.EndOfFileToken);
        return new SyntaxTree(Errors, _references, expression, eof);
    }

    private ExpressionSyntax ParseExpression()
    {
        return ParseBinaryExpression();
    }

    /// <summary>
    ///     Parse binary expression with given parent precedence
    /// </summary>
    /// <param name="parentPrecedence"></param>
    /// <returns></returns>
    private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        // Determine the precedence of the unary operator (returns 0 if it's not a 
        // unary operator
        var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();

        if (unaryOperatorPrecedence != 0 &&
            unaryOperatorPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseBinaryExpression(unaryOperatorPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }


        while (true)
        {
            var precedence = Current.Kind.GetBinaryOperatorPrecedence();
            // If not a binary operator or if the precedence is less than the parent, exit loop
            if (precedence == 0 || precedence <= parentPrecedence)
                break;

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    /// <summary>
    /// Parse primary expression (true, false, identifier)
    /// </summary>
    /// <returns></returns>
    private ExpressionSyntax ParsePrimaryExpression()
    {
        switch (Current.Kind)
        {
            case SyntaxKind.LeftParenthesisToken:
            {
                var left = NextToken();
                var expression = ParseExpression();
                var right = MatchToken(SyntaxKind.RightParenthesisToken);
                return new ParenthesizedExpressionSyntax(left, expression, right);
            }
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            {
                var keywordToken = NextToken();
                var value = keywordToken.Kind == SyntaxKind.TrueKeyword;
                return new LiteralExpressionSyntax(keywordToken, value);
            }
            case SyntaxKind.IdentifierToken:
            {
                return parseIdentifierToken();
            }
            case SyntaxKind.StringToken:
                return new LiteralExpressionSyntax(NextToken());
            default:
            {
                if (Current.Kind == SyntaxKind.NumberToken &&
                    Peek(1).Kind == SyntaxKind.ColonToken &&
                    (Peek(2).Kind is SyntaxKind.NumberToken or SyntaxKind.IdentifierToken))
                {
                    return parseRange();
                }

                return new LiteralExpressionSyntax(MatchToken(SyntaxKind.NumberToken));
            }
        }
    }

    private ExpressionSyntax parseIdentifierToken()
    {
        if (Peek(1).Kind == SyntaxKind.LeftParenthesisToken)
        {
            return parseFunctionCall();
        }

        if (Peek(1).Kind == SyntaxKind.ColonToken &&
            (Peek(2).Kind == SyntaxKind.IdentifierToken ||
             Peek(2).Kind == SyntaxKind.NumberToken))
        {
            return parseRange();
        }

        if (isValidCellReference(Current.Text))
            return parseCell();

        var identifier = NextToken();
        _references.Add(new NamedReference(identifier.Text));
        return new NameExpressionSyntax(identifier);
    }

    private ExpressionSyntax parseRange()
    {
        var start = ParseRangePartAsReference(NextToken());
        var colon = NextToken();
        var end = ParseRangePartAsReference(NextToken());

        var rangeReference = new RangeReference(start, end);
        _references.Add(rangeReference);
        return new RangeReferenceExpressionSyntax(rangeReference);
    }

    private Reference ParseRangePartAsReference(SyntaxToken token)
    {
        if (isValidCellReference(token.Text))
        {
            var cellReference = CellReference.FromString(token.Text);
            return cellReference;
        }

        if (isValidColReference(token.Text))
            return new ColReference(CellReference.ColStrToNumber(token.Text),
                                    token.Text.StartsWith('$'));

        if (isValidRowReference(token.Text))
            return new RowReference(CellReference.RowStrToNumber(token.Text), token.Text.StartsWith('$'));

        return new NamedReference(token.Text);
    }

    private bool isValidColReference(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '$' && i == 0)
                continue;
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    private bool isValidRowReference(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '$' && i == 0)
                continue;
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    private ExpressionSyntax parseCell()
    {
        var cellToken = NextToken();
        var cellReference = CellReference.FromString(cellToken.Text);
        _references.Add(cellReference);
        return new CellExpressionSyntax(cellReference);
    }

    private bool isValidCellReference(string text)
    {
        return CellReference.IsValid(text);
    }

    private ExpressionSyntax parseFunctionCall()
    {
        // Consume left param
        var identifierToken = NextToken();
        var leftParenth = MatchToken(SyntaxKind.LeftParenthesisToken);

        // Collect arguments
        var args = new List<ExpressionSyntax>();
        // If no args
        if (Current.Kind != SyntaxKind.RightParenthesisToken)
        {
            while (true)
            {
                args.Add(ParseExpression());
                if (Current.Kind == SyntaxKind.CommaToken)
                {
                    NextToken();
                    continue;
                }
                else
                    break;
            }
        }

        MatchToken(SyntaxKind.RightParenthesisToken);

        return new FunctionCallExpressionSyntax(identifierToken, args);
    }

    /// <summary>
    /// Look at (but don't consume) the token at the offset specified
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _tokens.Length)
            return _tokens[^1];

        return _tokens[index];
    }

    /// <summary>
    /// Consumes and returns the next token
    /// </summary>
    /// <returns></returns>
    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    /// <summary>
    ///     Return the next token if it matches the token kind provided, otherwise flag an error
    ///     and return a token of the kind provided
    /// </summary>
    /// <param name="kind"></param>
    /// <returns></returns>
    private SyntaxToken MatchToken(SyntaxKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();

        Errors.Add($"ERROR: Unexpected token: <{Current.Kind}>. Expected {kind}");
        return new SyntaxToken(kind, Current.Position, null, null);
    }
}