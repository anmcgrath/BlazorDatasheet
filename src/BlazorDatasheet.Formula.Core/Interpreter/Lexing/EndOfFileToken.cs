namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class EndOfFileToken : Token
{
    public EndOfFileToken(int positionStart) : base(Tag.Eof, positionStart)
    {
    }
}