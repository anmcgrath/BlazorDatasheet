namespace BlazorDatasheet.DataStructures.Geometry;

public class Rect : IEquatable<Rect>
{
    public double X { get; }
    public double Height { get; }
    public double Y { get; }
    public double Width { get; }

    public double Right => X + Width;
    public double Bottom => Y + Height;

    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Height = height;
        Y = y;
        Width = width;
    }

    public Rect? GetIntersection(Rect? rect)
    {
        if (rect == null)
            return null;

        var x1 = this.Right;
        var x2 = rect.Right;
        var y1 = this.Bottom;
        var y2 = rect.Bottom;

        Rect? intersection;

        var xL = Math.Max(this.X, rect.X);
        var xR = Math.Min(x1, x2);
        if (xR < xL)
            intersection = null;
        else
        {
            var yB = Math.Min(y1, y2);
            var yT = Math.Max(this.Y, rect.Y);
            if (yB < yT)
                intersection = null;
            else
                intersection = new Rect(xL, yT, xR - xL, yB - yT);
        }

        return intersection;
    }

    public bool Equals(Rect? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return X == other.X &&
               Y == other.Y &&
               Width == other.Width &&
               Height == other.Height;
    }

    public override bool Equals(object? obj)
    {
        return obj is Rect other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height);
    }

    public static bool operator ==(Rect? left, Rect? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Rect? left, Rect? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}, Width: {Width}, Height: {Height}";
    }
}
