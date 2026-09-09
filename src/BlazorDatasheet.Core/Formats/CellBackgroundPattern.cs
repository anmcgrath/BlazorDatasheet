namespace BlazorDatasheet.Core.Formats;

/// <summary>A repeating background drawn over the cell background color. Sizes are in pixels.</summary>
public sealed record CellBackgroundPattern(
    CellBackgroundPatternKind Kind,
    string Color = "currentColor",
    double Spacing = 8,
    double StrokeSize = 1);

/// <summary>Built-in repeating cell backgrounds.</summary>
public enum CellBackgroundPatternKind
{
    Horizontal,
    Vertical,
    Diagonal,
    Crosshatch,
    Dots
}
