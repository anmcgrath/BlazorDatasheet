namespace BlazorDatasheet.Formula.Core.Interpreter2.Lexing;

public ref struct Lexer
{
    private int _position;
    private ReadOnlySpan<char> _string;
    private char _current;
    public List<string> Errors { get; private set; } = null!;

    public Lexer()
    {
        _position = 0;
        _string = default;
        _current = '\0';
    }

    public Token[] Lex(string text)
    {
        _string = text.AsSpan();
        _position = 0;
        _current = _string[0];
        Errors = new();

        var tokens = new List<Token>();
        Token currentToken;
        while ((currentToken = ReadToken()).Tag != Tag.Eof)
        {
            tokens.Add(currentToken);
        }

        tokens.Add(new EndOfFileToken(_position));

        return tokens.ToArray();
    }

    private Token ReadToken()
    {
        while (char.IsWhiteSpace(_current))
            Next();

        if (_current == '\0')
            return new EndOfFileToken(_position);

        if (char.IsDigit(_current))
            return ReadNumber(false);
        else if (_current == '.')
            return ReadNumber(true);

        if (char.IsLetter(_current) || _current == '$')
            return ReadIdentifier();

        if (_current == '"')
            return ReadString(_string);

        Token token;
        switch (_current)
        {
            case '=':
                token = new Token(Tag.EqualsToken, _position);
                break;
            case '+':
                token = new Token(Tag.PlusToken, _position);
                break;
            case '-':
                token = new Token(Tag.MinusToken, _position);
                break;
            case '/':
                token = new Token(Tag.SlashToken, _position);
                break;
            case '*':
                token = new Token(Tag.StarToken, _position);
                break;
            case ':':
                token = new Token(Tag.ColonToken, _position);
                break;
            case '&':
                token = new Token(Tag.AmpersandToken, _position);
                break;
            case '>':
                if (Peek(1) == '=')
                {
                    token = new Token(Tag.GreaterThanOrEqualToToken, _position);
                    Next();
                }
                else
                    token = new Token(Tag.GreaterThanToken, _position - 1);

                break;
            case '<':
                if (Peek(1) == '=')
                {
                    token = new Token(Tag.LessThanOrEqualToToken, _position);
                    Next();
                }
                else if (Peek(1) == '>')
                {
                    token = new Token(Tag.NotEqualToToken, _position);
                    Next();
                }
                else
                    token = new Token(Tag.LessThanToken, _position - 1);

                break;
            case '(':
                token = new Token(Tag.LeftParenthToken, _position);
                break;
            case ')':
                token = new Token(Tag.RightParenthToken, _position);
                break;
            case ',':
                token = new Token(Tag.CommaToken, _position);
                break;
            case '{':
                token = new Token(Tag.LeftCurlyBracketToken, _position);
                break;
            case '}':
                token = new Token(Tag.RightCurlyBracketToken, _position);
                break;
            case '!':
                token = new Token(Tag.BangToken, _position);
                break;
            case ';':
                token = new Token(Tag.SemiColonToken, _position);
                break;
            default:
                token = new BadToken(_position);
                break;
        }

        Next();
        return token;
    }

    private Token ReadNumber(bool containsPeriod)
    {
        int start = _position;
        Next();

        while ((char.IsDigit(_current) || _current == '.'))
        {
            if (_current == '.')
                containsPeriod = true;
            Next();
        }

        int length = _position - start;

        if (containsPeriod) // could be double
        {
            if (double.TryParse(_string.Slice(start, length), out var parsedDouble))
                return new NumberToken(parsedDouble, start);

            Error($"{_string.Slice(start, length).ToString()} is not a number");
            return new BadToken(_position);
        }

        if (int.TryParse(_string.Slice(start, length), out var parsedInt))
        {
            return new NumberToken(parsedInt, start);
        }

        return new BadToken(_position);
    }

    private IdentifierToken ReadIdentifier()
    {
        int start = _position;
        Next(); // consume start character
        while (char.IsLetterOrDigit(_current) || _current == '$')
            Next();
        int length = _position - start;

        var identifier = _string.Slice(start, length).ToString();
        return new IdentifierToken(identifier, start);
    }

    private Token ReadString(ReadOnlySpan<char> str)
    {
        int start = _position;
        // Consume first ""
        Next();

        if (_current == '\0')
        {
            Error("End of file reached unexpectedly");
            return new BadToken(start);
        }

        while (_current != '"')
        {
            if (_current == '\0')
            {
                Error($"Found EOF");
                return new BadToken(start);
            }

            Next();
        }

        int length = _position - start - 1;
        var stringValue = str.Slice(start + 1, length).ToString();

        // consume the last ""
        Next();

        return new StringToken(stringValue, start);
    }

    private void Error(string error)
    {
        Errors.Add($"Error at position {_position}: {error}");
    }

    private void Next()
    {
        _position++;
        if (_position > _string.Length - 1)
            _current = '\0';
        else
            _current = _string[_position];
    }

    private char Peek(int offset)
    {
        if (_position + offset > _string.Length - 1)
            return '\0';
        return _string[_position + offset];
    }
}