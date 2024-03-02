namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class StringToken : Token
{
    public string Value { get; }

    public StringToken(string value, int startPosition) : base(Tag.StringToken, startPosition)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}