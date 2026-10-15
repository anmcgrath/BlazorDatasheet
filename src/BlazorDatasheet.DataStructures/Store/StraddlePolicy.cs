namespace BlazorDatasheet.DataStructures.Store;

/// <summary>
/// How a region store treats a region that extends outside the band of a band-limited cell shift.
/// </summary>
public enum StraddlePolicy
{
    /// <summary>
    /// The region is split at the band boundary; only the part inside the band is shifted.
    /// </summary>
    Split,

    /// <summary>
    /// The region is left unchanged.
    /// </summary>
    Keep
}
