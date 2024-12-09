namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public class QuotedSheetNameToken : Token
{
    public string Text { get; }

    public QuotedSheetNameToken(string text, int positionStart) : base(Tag.QuotedSheetName, text, positionStart)
    {
        Text = text;
    }
}