namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class WhitespaceToken : Token
{
    public WhitespaceToken(string text, int positionStart) : base(Tag.Whitespace, text, positionStart)
    {
    }
}