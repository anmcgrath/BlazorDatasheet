namespace BlazorDatasheet.Core.Data;

public sealed record FreezeState
{
    /// <summary>
    /// The number of frozen rows at the top of the sheet
    /// </summary>
    public int Top { get; }

    /// <summary>
    /// The number of frozen rows at the bottom of the sheet
    /// </summary>
    public int Bottom { get; }

    /// <summary>
    /// The number of frozen columns at the left of the sheet
    /// </summary>
    public int Left { get; }

    /// <summary>
    /// The number of frozen columns at the right of the sheet
    /// </summary>
    public int Right { get; }

    internal FreezeState(int top, int bottom, int left, int right)
    {
        Top = top;
        Bottom = bottom;
        Left = left;
        Right = right;
    }
}