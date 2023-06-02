using System.Text;

namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

internal class Lexer
{
    private string _text;
    private readonly List<string> _errors = new();
    private int _position;

    public IEnumerable<string> Errors => _errors;

    private char Current => Peek(0);

    private char Lookahead => Peek(1);

    private char Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _text.Length)
            return '\0';
        return _text[index];
    }

    private void Next()
    {
        _position++;
    }

    public void Begin(string text)
    {
        _text = text;
        _errors.Clear();
        _position = 0;
    }

    /// <summary>
    /// Return the next token
    /// </summary>
    /// <returns></returns>
    public SyntaxToken Lex()
    {
        if (Current == '\0')
            return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);

        var start = _position;

        // Numbers
        if (char.IsDigit(Current) || Current == '.')
        {
            while (char.IsDigit(Current) || Current == '.')
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);

            if (!double.TryParse(text, out var value))
                _errors.Add($"The number {_text} cannot be represented by a double");
            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }

        // Whitespace
        if (char.IsWhiteSpace(Current))
        {
            while (char.IsWhiteSpace(Current))
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, text);
        }

        // Words / cell references
        if (char.IsLetter(Current) || Current == '$')
        {
            while (char.IsLetterOrDigit(Current) || Current == '$')
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            var kind = SyntaxFacts.GetKeywordKind(text);
            return new SyntaxToken(kind, start, text, null);
        }

        switch (Current)
        {
            case '+':
                return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
            case '-':
                return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
            case '/':
                return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
            case '*':
                return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
            case '(':
                return new SyntaxToken(SyntaxKind.LeftParenthesisToken, _position++, "(", null);
            case ')':
                return new SyntaxToken(SyntaxKind.RightParenthesisToken, _position++, ")", null);
            case '!':
                return new SyntaxToken(SyntaxKind.BangToken, _position++, "!", null);
            case '<':
                if (Lookahead == '>')
                {
                    _position += 2;
                    return new SyntaxToken(SyntaxKind.NotEqualsToken, _position, "<>", null);
                }

                if (Lookahead == '=')
                {
                    _position += 2;
                    return new SyntaxToken(SyntaxKind.LessThanEqualToToken, _position, "<=", null);
                }

                return new SyntaxToken(SyntaxKind.LessThanToken, _position++, "<", null);
            case '>':

                if (Lookahead == '=')
                {
                    _position += 2;
                    return new SyntaxToken(SyntaxKind.GreaterThanEqualToToken, _position, "<=", null);
                }

                return new SyntaxToken(SyntaxKind.GreaterThanToken, _position++, ">", null);
            case '&':
                if (Lookahead == '&')
                {
                    _position += 2;
                    return new SyntaxToken(SyntaxKind.AmpersandAmpersandToken, _position, "&&", null);
                }

                break;
            case '|':
                if (Lookahead == '|')
                {
                    _position += 2;
                    return new SyntaxToken(SyntaxKind.PipePipeToken, _position, "||", null);
                }

                break;
            case '=':
                if (Lookahead == '=')
                {
                    _position += 2;
                    return new SyntaxToken(SyntaxKind.EqualsEqualsToken, start, "==", null);
                }

                return new SyntaxToken(SyntaxKind.EqualsToken, _position++, "=", null);
            case ':':
                return new SyntaxToken(SyntaxKind.ColonToken, _position++, ":", null);
            case ',':
                return new SyntaxToken(SyntaxKind.CommaToken, _position++, ",", null);
            case '"':
                return readString();
        }

        _errors.Add($"ERROR: bad character input: {Current} at position {_position + 1}");
        return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1), 1);
    }

    private SyntaxToken readString()
    {
        // Move to letter after first quote
        var start = ++_position;
        var sb = new StringBuilder();
        var done = false;
        while (!done)
        {
            switch (Current)
            {
                case '\0':
                {
                    _errors.Add($"Unterminated string at position {start}");
                    done = true;
                    break;
                }
                case '"':
                {
                    done = true;
                    _position++;
                    break;
                }
                default:
                {
                    sb.Append(Current);
                    _position++;
                    break;
                }
            }
        }

        var text = sb.ToString();
        return new SyntaxToken(SyntaxKind.StringToken, start, $"\"{text}\"", text);
    }
}