namespace BlazorDatasheet.DataStructures.Geometry;

public struct Size
{
    public double Width { get; }
    public double Height { get; }

    public Size(double width, double height)
    {
        Width = width;
        Height = height;
    }
}