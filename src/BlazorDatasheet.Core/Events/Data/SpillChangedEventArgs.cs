using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Data;

/// <summary>
/// Raised when the region an array formula spills into is set, changed or removed.
/// </summary>
public class SpillChangedEventArgs
{
    /// <summary>The cell holding the array formula.</summary>
    public CellPosition Anchor { get; }

    /// <summary>The region spilled into before the change, or null if it had not spilled.</summary>
    public IRegion? OldRegion { get; }

    /// <summary>The region spilled into after the change, or null if it no longer spills.</summary>
    public IRegion? NewRegion { get; }

    public SpillChangedEventArgs(CellPosition anchor, IRegion? oldRegion, IRegion? newRegion)
    {
        Anchor = anchor;
        OldRegion = oldRegion;
        NewRegion = newRegion;
    }
}
