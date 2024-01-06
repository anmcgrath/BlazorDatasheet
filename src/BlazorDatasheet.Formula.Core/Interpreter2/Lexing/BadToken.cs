namespace BlazorDatasheet.Formula.Core.Interpreter2.Lexing;

public class BadToken : Token
{
    public BadToken(int positionStart) : base(Tag.BadToken, positionStart)
    {
    }
}