namespace BlazorDatasheet.DataStructures.Geometry;

public struct Point2d
{
    public double X { get; }
    public double Y { get; }

    public Point2d(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}";
    }
}