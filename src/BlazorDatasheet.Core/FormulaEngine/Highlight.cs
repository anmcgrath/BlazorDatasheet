namespace BlazorDatasheet.Core.FormulaEngine;

public class Highlight
{
    public int StartPosition { get; }
    public int EndPosition { get; }
    public System.Drawing.Color Color { get; }

    public Highlight(int startPosition, int endPosition, System.Drawing.Color color)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        Color = color;
    }
}