namespace BlazorDatasheet.Formula.Core.Interpreter2.Lexing;

public class Token
{
    public Tag Tag { get; }
    public int PositionStart { get; }

    public Token(Tag tag, int positionStart)
    {
        Tag = tag;
        PositionStart = positionStart;
    }

    public override string ToString()
    {
        return $"{Tag}@{PositionStart}";
    }
}