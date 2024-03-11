namespace BlazorDatasheet.DataStructures.Geometry;

public struct Rect
{
    public double X { get; }
    public double Height { get; }
    public double Y { get; }
    public double Width { get; }

    public Rect(double x, double y, double width, double height)
    {
        X = x;
        Height = height;
        Y = y;
        Width = width;
    }
}