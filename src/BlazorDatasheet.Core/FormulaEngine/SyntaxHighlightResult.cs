namespace BlazorDatasheet.Core.FormulaEngine;

public class SyntaxHighlightResult
{
    public ICollection<Highlight> Highlights { get; }

    public SyntaxHighlightResult(ICollection<Highlight> highlights)
    {
        Highlights = highlights;
    }
}