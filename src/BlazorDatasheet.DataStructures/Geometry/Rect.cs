namespace BlazorDatasheet.DataStructures.Geometry;

public struct Rect
{
    public double Top { get; }
    public double Left { get; }
    public double Right { get; }
    public double Bottom { get; }

    public Rect(double top, double right, double bottom, double left)
    {
        Top = top;
        Left = left;
        Right = right;
        Bottom = bottom;
    }
}