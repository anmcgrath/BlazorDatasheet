namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class BadToken : Token
{
    public BadToken(int positionStart) : base(Tag.BadToken, positionStart)
    {
    }
}