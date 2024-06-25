using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public ref struct Lexer
{
    private int _position;
    private ReadOnlySpan<char> _string;
    private WhiteSpaceOptions _whiteSpaceOptions = WhiteSpaceOptions.RemoveWhitespace;
    private char _current;
    private LexerReferenceState _referenceState = LexerReferenceState.None;
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

        if (_current == '\0')
            return new EndOfFileToken(_position);

        if (char.IsDigit(_current))
            return ReadNumber(false);
        else if (_current == '.')
            return ReadNumber(true);

        if (char.IsLetter(_current) || _current == '$')
            return ReadIdentifier();

        if (_current == '"')
            return ReadStringLiteral();

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
            // if we are in the second part of a range parsing...
            if (_referenceState == LexerReferenceState.ReadingReference)
            {
                return new AddressToken(new RowAddress(parsedInt - 1, parsedInt, false), start);
            }

            // otherwise check if we are going to parse a range
            if (_current == ':' && _referenceState == LexerReferenceState.None)
            {
                _referenceState = LexerReferenceState.ReadingReference;
                // store posn just in case
                var tempPosition = _position;
                Next(); // consume colon

                var rightToken = ReadToken();
                _referenceState = LexerReferenceState.None;

                var rowStartAddr = new RowAddress(parsedInt - 1, parsedInt, false);

                if (rightToken.Tag == Tag.Number)
                {
                    var rightNumToken = (NumberToken)rightToken;
                    if (rightNumToken.IsInteger)
                    {
                        var rowEnd = new RowAddress((int)rightNumToken.Value - 1, (int)rightNumToken.Value, false);
                        return new AddressToken(new RangeAddress(rowStartAddr, rowEnd), start);
                    }
                }

                if (rightToken.Tag == Tag.AddressToken)
                {
                    var rightRefToken = (AddressToken)rightToken;
                    if (rightRefToken.Address.Kind == AddressKind.RowAddress)
                    {
                        return new AddressToken(new RangeAddress(rowStartAddr, rightRefToken.Address), start);
                    }
                }

                ResetPosition(tempPosition);
            }

            return new NumberToken(parsedInt, start);
        }

        return new BadToken(_position);
    }

    private Token ReadIdentifier()
    {
        int start = _position;
        Next(); // consume start character
        while (char.IsLetterOrDigit(_current) || _current == '$')
            Next();


        int length = _position - start;
        var idSlice = _string.Slice(start, length);

        // if the current identifier is a valid row, column or cell reference then
        // we look to see if it is part of a range (e.g 1:2, a:2, b2:b3 etc.)
        var canParseRef = RangeText.TryParseSingleAddress(idSlice, out var parsedLeftAddress);
        if (!canParseRef)
            return new IdentifierToken(idSlice.ToString(), start);

        if (parsedLeftAddress!.Kind == AddressKind.NamedAddress)
            return new IdentifierToken(idSlice.ToString(), start);

        if (canParseRef && _referenceState == LexerReferenceState.ReadingReference)
        {
            return new AddressToken(parsedLeftAddress, start);
        }

        if (_current == ':' && _referenceState == LexerReferenceState.None) // so we only look ahead one at most
        {
            _referenceState = LexerReferenceState.ReadingReference;

            // store temp position so we can come back to it
            var tempPosition = _position;
            var colon = ReadToken();
            var next = ReadToken();

            _referenceState = LexerReferenceState.None;

            if (next.Tag == Tag.AddressToken)
            {
                var rightToken = (AddressToken)next;
                if (rightToken.Address.Kind == parsedLeftAddress.Kind)
                {
                    return new AddressToken(new RangeAddress(parsedLeftAddress, rightToken.Address), start);
                }
            }

            // in this case we know the second row is not absolute reference
            if (next.Tag == Tag.Number && parsedLeftAddress.Kind == AddressKind.RowAddress)
            {
                var nextTokenAsNum = (NumberToken)next;
                if (nextTokenAsNum.IsInteger)
                {
                    var rowRefRight = new RowAddress((int)nextTokenAsNum.Value - 1, (int)nextTokenAsNum.Value, false);
                    return new AddressToken(new RangeAddress(parsedLeftAddress, rowRefRight), start);
                }
            }

            // otherwise reset our position
            ResetPosition(tempPosition);
        }

        if (canParseRef && parsedLeftAddress!.Kind == AddressKind.CellAddress)
            return new AddressToken(parsedLeftAddress, start);

        return new IdentifierToken(idSlice.ToString(), start);
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

    private void ResetPosition(int position)
    {
        _position = position;
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