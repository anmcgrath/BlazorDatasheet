using System.Globalization;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public ref struct Lexer
{
    private int _position;
    private ReadOnlySpan<char> _string;
    private WhiteSpaceOptions _whiteSpaceOptions = WhiteSpaceOptions.RemoveWhitespace;
    private char _current;
    public List<string> Errors { get; private set; } = null!;

    public Lexer()
    {
        _position = 0;
        _string = default;
        _current = '\0';
    }

    public Token[] Lex(string text, WhiteSpaceOptions whiteSpaceOptions = WhiteSpaceOptions.RemoveWhitespace)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<Token>();

        _string = text.AsSpan();
        _position = 0;
        _current = _string[0];
        _whiteSpaceOptions = whiteSpaceOptions;
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

    private char GetNumberSeparatorChar() => Convert.ToChar(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
    private char GetNegativeSign() => Convert.ToChar(NumberFormatInfo.CurrentInfo.NegativeSign);

    private Token ReadToken()
    {
        if (char.IsWhiteSpace(_current))
        {
            int start = _position;
            Next();
            while (char.IsWhiteSpace(_current))
            {
                Next();
            }

            var len = _position - start;

            if (_whiteSpaceOptions == WhiteSpaceOptions.PreserveAll)
                return new WhitespaceToken(_string.Slice(start, len).ToString(), start);
        }

        if (_current == '#')
            return ReadErrorToken();

        if (_current == '\0')
            return new EndOfFileToken(_position);

        if (char.IsDigit(_current))
            return ReadNumber();

        if (_string.Slice(_position).StartsWith(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
            return ReadNumber();

        if (char.IsLetter(_current) || _current == '$')
            return ReadIdentifier();

        if (_current == '"')
            return ReadStringLiteral();

        if (_current == '\'')
            return ReadQuotedSheetName();

        Token token;
        switch (_current)
        {
            case '=':
                token = new Token(Tag.EqualsToken, "=", _position);
                break;
            case '+':
                token = new Token(Tag.PlusToken, "+", _position);
                break;
            case '-':
                token = new Token(Tag.MinusToken, "-", _position);
                break;
            case '/':
                token = new Token(Tag.SlashToken, "/", _position);
                break;
            case '*':
                token = new Token(Tag.StarToken, "*", _position);
                break;
            case ':':
                token = new Token(Tag.ColonToken, ":", _position);
                break;
            case '&':
                token = new Token(Tag.AmpersandToken, "&", _position);
                break;
            case '%':
                token = new Token(Tag.PercentToken, "%", _position);
                break;
            case '>':
                if (Peek(1) == '=')
                {
                    token = new Token(Tag.GreaterThanOrEqualToToken, ">=", _position);
                    Next();
                }
                else
                    token = new Token(Tag.GreaterThanToken, ">", _position);

                break;
            case '<':
                if (Peek(1) == '=')
                {
                    token = new Token(Tag.LessThanOrEqualToToken, "<=", _position);
                    Next();
                }
                else if (Peek(1) == '>')
                {
                    token = new Token(Tag.NotEqualToToken, "<>", _position);
                    Next();
                }
                else
                    token = new Token(Tag.LessThanToken, "<", _position);

                break;
            case '(':
                token = new Token(Tag.LeftParenthToken, "(", _position);
                break;
            case ')':
                token = new Token(Tag.RightParenthToken, ")", _position);
                break;
            case ',':
                token = new Token(Tag.CommaToken, ",", _position);
                break;
            case '{':
                token = new Token(Tag.LeftCurlyBracketToken, "{", _position);
                break;
            case '}':
                token = new Token(Tag.RightCurlyBracketToken, "}", _position);
                break;
            case '!':
                token = new Token(Tag.BangToken, "!", _position);
                break;
            case ';':
                token = new Token(Tag.SemiColonToken, ";", _position);
                break;
            default:
                token = new BadToken(_position);
                break;
        }

        Next();
        return token;
    }

    private Token ReadErrorToken()
    {
        int start = _position;
        while (char.IsLetter(_current) ||
               _current == '#' ||
               _current == '/' ||
               _current == '?' ||
               _current == '!' ||
               _current == '0')
            Next();

        int length = _position - start;
        var errorStr = _string.Slice(start, length);
        var errorType = ErrorTypes.FromErrorString(errorStr);
        if (errorType == ErrorType.None)
        {
            Error("Invalid error token");
            return new BadToken(start);
        }

        return new ErrorToken(errorType, start);
    }

    private Token ReadNumber()
    {
        int start = _position;
        bool containsE = false;
        Next();

        while (char.IsDigit(_current) ||
               _current == GetNumberSeparatorChar() || _current == 'e' || (containsE && _current == GetNegativeSign()))
        {
            if (_current == 'e')
                containsE = true;

            Next();
        }

        int length = _position - start;

        if (int.TryParse(_string.Slice(start, length), out var parsedInt))
            return new NumberToken(parsedInt, start);

        if (double.TryParse(_string.Slice(start, length), out var parsedDouble))
            return new NumberToken(parsedDouble, start);

        Error($"{_string.Slice(start, length).ToString()} is not a number");
        return new BadToken(_position);
    }

    private Token ReadIdentifier()
    {
        int start = _position;
        Next(); // consume start character
        while (RangeText.IsValidNameChar(_current) || _current == '$')
            Next();

        int length = _position - start;
        var idSlice = _string.Slice(start, length);

        if (bool.TryParse(idSlice, out var parsedBool))
            return new LogicalToken(idSlice.ToString(), parsedBool, start);

        if (Peek(0) == '!')
        {
            // consume '!'
            Next();
            return new SheetLocatorToken(idSlice.ToString(), start);
        }

        // if the current identifier is a valid row, column or cell reference then
        // we look to see if it is part of a range (e.g 1:2, a:2, b2:b3 etc.)
        var canParseRef = RangeText.TryParseSingleAddress(idSlice, out var parsedLeftAddress);
        if (!canParseRef)
            return new IdentifierToken(idSlice.ToString(), start);

        if (parsedLeftAddress!.Kind == AddressKind.NamedAddress)
            return new IdentifierToken(idSlice.ToString(), start);

        if (canParseRef &&
            parsedLeftAddress.Kind == AddressKind.CellAddress ||
            parsedLeftAddress.Kind == AddressKind.ColAddress ||
            parsedLeftAddress.Kind == AddressKind.RowAddress)
            return new AddressToken(parsedLeftAddress, start);

        return new IdentifierToken(idSlice.ToString(), start);
    }

    private Token ReadQuotedSheetName()
    {
        int start = _position;
        // Consume first '
        Next();

        if (_current == '\0')
        {
            Error("End of file reached unexpectedly");
            return new BadToken(start);
        }

        while (_current != '\'')
        {
            if (_current == '\0')
            {
                Error($"Found EOF");
                return new BadToken(start);
            }

            Next();
        }

        int length = _position - start - 1;
        var stringValue = _string.Slice(start + 1, length).ToString();

        // consume the last '
        Next();

        if (Peek(0) == '!')
        {
            // consume '!'
            Next();
            return new SheetLocatorToken(stringValue, start);
        }

        return new BadToken(_position);
    }

    private Token ReadStringLiteral()
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
        var stringValue = _string.Slice(start + 1, length).ToString();

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