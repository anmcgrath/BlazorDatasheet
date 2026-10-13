namespace BlazorDatasheet.Core.Clipboard;

/// <summary>
/// What a paste writes into the target.
/// </summary>
public enum PasteMode
{
    /// <summary>
    /// Values, formulas, formats, merges and types.
    /// </summary>
    All,

    /// <summary>
    /// Only the (calculated) values.
    /// </summary>
    Values,

    /// <summary>
    /// Only formatting and merges.
    /// </summary>
    Formats
}
