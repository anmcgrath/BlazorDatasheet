namespace BlazorDatasheet.Formula.Core.Interpreter2.Lexing;

public class EndOfFileToken : Token
{
    public EndOfFileToken(int positionStart) : base(Tag.Eof, positionStart)
    {
    }
}