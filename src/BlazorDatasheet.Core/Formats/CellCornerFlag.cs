namespace BlazorDatasheet.Core.Formats;

/// <summary>A decorative corner triangle. Size is in pixels and does not affect cell layout.</summary>
public sealed record CellCornerFlag(string Color = "currentColor", double Size = 8);
