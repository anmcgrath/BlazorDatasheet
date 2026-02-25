namespace BlazorDatasheet.DataStructures.Geometry;

public struct Point2d
{
    public double X { get; init; }
    public double Y { get; init; }

    public Point2d(double x, double y)
    {
        X = x;
        Y = y;
    }

    public Point2d()
    {
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}";
    }
}