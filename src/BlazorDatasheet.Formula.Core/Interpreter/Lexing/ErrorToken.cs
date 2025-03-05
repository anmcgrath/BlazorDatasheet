namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class ErrorToken : Token
{
    public ErrorType ErrorType { get; }

    public ErrorToken(ErrorType errorType, int positionStart) : base(Tag.ErrorToken, positionStart)
    {
        ErrorType = errorType;
    }
}