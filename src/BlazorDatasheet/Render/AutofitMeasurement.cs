using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

internal class AutofitMeasurement
{
    public int Row { get; set; }
    public int Col { get; set; }
    public required Size Size { get; set; }
}