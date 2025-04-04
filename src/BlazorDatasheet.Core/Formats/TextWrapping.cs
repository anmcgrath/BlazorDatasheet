namespace BlazorDatasheet.Core.Formats;

public enum TextWrapping
{
    /// <summary>
    /// Text is wrapped inside the cell.
    /// </summary>
    Wrap = 1,

    /// <summary>
    /// Text is clipped and will not extend outside the cell.
    /// </summary>
    Clip = 0
}