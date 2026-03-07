using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render.Layers.Preview;

/// <summary>
/// Base type for preview item coordinates. Either cell-based (<see cref="RegionCoord"/>)
/// or pixel-based (<see cref="PixelCoord"/>).
/// </summary>
public abstract record PreviewCoord;

/// <summary>
/// Cell-based coordinates defined by a sheet region. The preview layer clips and converts
/// the region to pixel positions relative to each pane's view region.
/// </summary>
/// <param name="Region">The cell region to highlight.</param>
public sealed record RegionCoord(IRegion Region) : PreviewCoord;

/// <summary>
/// Pixel-based coordinates in sheet-absolute space. The preview layer converts these
/// to layer-relative positions for rendering.
/// </summary>
/// <param name="X">Left edge in sheet-absolute pixels.</param>
/// <param name="Y">Top edge in sheet-absolute pixels.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public sealed record PixelCoord(double X, double Y, double Width, double Height) : PreviewCoord;

/// <summary>
/// A non-interactive visual overlay (line, box) rendered by <see cref="PreviewLayer"/>
/// across all panes. Used for features like column resize lines and autofill drag previews.
/// </summary>
public class PreviewItem
{
    /// <summary>
    /// The position and size of the preview, either cell-based or pixel-based.
    /// </summary>
    public PreviewCoord Coord { get; set; } = null!;

    /// <summary>
    /// CSS border/outline color.
    /// </summary>
    public string BorderColor { get; set; } = "var(--selection-border-color)";

    /// <summary>
    /// CSS border style (e.g. "solid", "dashed").
    /// </summary>
    public string BorderStyle { get; set; } = "solid";

    /// <summary>
    /// CSS border thickness (e.g. "1px", "var(--selection-border-thickness)").
    /// </summary>
    public string BorderThickness { get; set; } = "1px";

    /// <summary>
    /// CSS background color. Only rendered when <see cref="BackgroundVisible"/> is true.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Whether to fill the overlay with <see cref="BackgroundColor"/>.
    /// </summary>
    public bool BackgroundVisible { get; set; }

    /// <summary>
    /// CSS z-index for stacking order within the preview layer.
    /// </summary>
    public int ZIndex { get; set; }
}
