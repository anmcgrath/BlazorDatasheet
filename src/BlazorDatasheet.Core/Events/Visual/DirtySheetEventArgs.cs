using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Events.Visual;

public class DirtySheetEventArgs
{
    /// <summary>
    /// The row spans that need re-rendering.
    /// </summary>
    public Range1DStore<bool> DirtyRows { get; init; } = default!;

    /// <summary>
    /// The first column spanned by anything marked dirty.
    /// </summary>
    public int DirtyColStart { get; init; } = int.MaxValue;

    /// <summary>
    /// The last column spanned by anything marked dirty. See <see cref="DirtyColStart"/>.
    /// </summary>
    public int DirtyColEnd { get; init; } = int.MinValue;

    /// <summary>
    /// Whether anything was actually marked dirty.
    /// </summary>
    public bool IsEmpty => DirtyColEnd < DirtyColStart;
}
