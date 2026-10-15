using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Edit;

public enum ReferenceDragMode
{
    /// <summary>
    /// The reference keeps its size and follows the pointer.
    /// </summary>
    Move,

    /// <summary>
    /// One corner of the reference follows the pointer, and the opposite corner stays where it is.
    /// </summary>
    Resize
}

/// <summary>
/// Calculates the region that a reference in a formula refers to, as it is dragged over the sheet.
/// Both calculations start from the region as it was when the drag began, so that a drag that comes back
/// to where it started gives back the region it started with.
/// </summary>
public static class ReferenceDragCalculator
{
    public static IRegion Move(IRegion original, CellPosition grab, CellPosition pointer, IRegion bounds) =>
        Selecting.RegionDragCalculator.Move(original, grab, pointer, bounds);

    public static IRegion Resize(IRegion original, CellPosition anchor, CellPosition pointer, IRegion bounds) =>
        Selecting.RegionDragCalculator.Resize(original, anchor, pointer, bounds);
}
