namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class Token
{
    public Tag Tag { get; }
    public string Text { get; } = null;
    public int PositionStart { get; }

    protected Token(Tag tag, int positionStart)
    {
        Tag = tag;
        PositionStart = positionStart;
    }

    public Token(Tag tag, string text, int positionStart)
    {
        Tag = tag;
        Text = text;
        PositionStart = positionStart;
    }

    public override string ToString()
    {
        return this.Text;
    }
}