namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class SheetLocatorToken : Token
{
    public SheetLocatorToken(string text, int positionStart) : base(Tag.SheetLocatorToken, text, positionStart)
    {
    }
}