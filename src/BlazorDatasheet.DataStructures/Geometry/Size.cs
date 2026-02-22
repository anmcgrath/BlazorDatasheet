namespace BlazorDatasheet.DataStructures.Geometry;

public class Size : IEquatable<Size>
{
    public double Width { get; }
    public double Height { get; }

    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Get(Axis axis)
    {
        return axis == Axis.Col ? Width : Height;
    }

    public bool Equals(Size? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Width == other.Width && Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is Size other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static bool operator ==(Size? left, Size? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Size? left, Size? right)
    {
        return !Equals(left, right);
    }
}
