namespace BlazorDatasheet.Render;

/// <summary>
/// What a number without an explicit number format does when it does not fit inside its cell.
/// </summary>
public enum NumberOverflowMode
{
    /// <summary>
    /// The number is rounded to fewer decimal places until it fits, as it is in Excel, and
    /// replaced with hashes once no permitted rounding fits.
    /// </summary>
    RoundToFit,

    /// <summary>
    /// The number is never rounded. It is replaced with hashes when it does not fit.
    /// </summary>
    Hashes,

    /// <summary>
    /// The number is clipped at the edge of the cell, which may slice or hide digits.
    /// </summary>
    Clip
}

