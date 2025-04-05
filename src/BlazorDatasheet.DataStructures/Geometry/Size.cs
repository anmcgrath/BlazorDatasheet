namespace BlazorDatasheet.DataStructures.Geometry;

public class Size
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
}