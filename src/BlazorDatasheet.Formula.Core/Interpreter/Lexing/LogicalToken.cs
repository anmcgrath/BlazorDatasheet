namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class LogicalToken : Token
{
    public bool Value { get; }

    public LogicalToken(string text, bool value, int positionStart) : base(Tag.LogicalToken, text, positionStart)
    {
        Value = value;
    }
}