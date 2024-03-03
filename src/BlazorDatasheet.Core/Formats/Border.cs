using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Formats;

public class Border : IMergeable<Border>, IEquatable<Border>
{
    public int? Width { get; set; }
    public string Color { get; set; }

    public void Merge(Border item)
    {
        if (!string.IsNullOrEmpty(item.Color))
            Color = item.Color;
        if (item.Width.HasValue)
            Width = item.Width;
    }

    public Border Clone()
    {
        return new Border()
        {
            Width = Width,
            Color = Color
        };
    }

    public bool Equals(Border? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Width == other.Width && Color == other.Color;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Color);
    }
}