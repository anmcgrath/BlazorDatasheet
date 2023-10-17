using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

/// <summary>
/// Provides information about the current "View" of the datasheet.
/// </summary>
public class Viewport
{
    /// <summary>
    /// The visible region, including the cells that are shown off-screen for performance improvements.
    /// </summary>
    public Region VisibleRegion { get; init; } = new(-1, -1, -1, -1);

    /// <summary>
    /// The left (x) position of the first cell in the visual region.
    /// </summary>
    public double Left { get; init; }

    /// <summary>
    /// The top (y) position of the first cell in the visual region
    /// </summary>
    public double Top { get; init; }

    /// <summary>
    /// The distance from the right edge of the viewport to the end of the sheet
    /// </summary>
    public double DistanceRight { get; init; }

    /// <summary>
    /// The distance from the bottom edge of the viewport to the end of the sheet.
    /// </summary>
    public double DistanceBottom { get; init; }
}