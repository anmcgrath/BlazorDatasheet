namespace BlazorDatasheet.Core.Formats;

public enum TextWrapping
{
    /// <summary>
    /// Text is allowed to overflow into non-empty cells.
    /// </summary>
    Overflow,
    /// <summary>
    /// Text is wrapped inside the cell.
    /// </summary>
    Wrap,
    /// <summary>
    /// Text is clipped and will not extend outside the cell.
    /// </summary>
    Clip
}