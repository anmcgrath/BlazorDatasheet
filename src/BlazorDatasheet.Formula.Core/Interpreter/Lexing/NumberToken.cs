using System.Globalization;

namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class NumberToken : Token
{
    public double Value { get; }
    public bool IsInteger { get; }

    public NumberToken(double value, int positionStart) : base(Tag.Number, positionStart)
    {
        Value = value;
    }

    public NumberToken(int value, int positionStart) : base(Tag.Number, positionStart)
    {
        Value = value;
        IsInteger = true;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}