namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class IdentifierToken : Token
{
    public string Value { get; }

    internal IdentifierToken(string value, int positionStart) : base(Tag.IdentifierToken, positionStart)
    {
        Value = value;
    }

    internal IdentifierToken(Tag tag, string value, int positionStart) : base(tag, positionStart)
    {
        Value = value;
    }

    public override string ToString()
    {
        return Value;
    }
}