namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class OperatorToken : Token
{
    public string Text { get; }

    public OperatorToken(Tag tag, string text, int positionStart) : base(tag, positionStart)
    {
        Text = text;
    }

    public override string ToString()
    {
        return Text;
    }
}