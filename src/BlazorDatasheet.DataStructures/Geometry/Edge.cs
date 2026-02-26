namespace BlazorDatasheet.DataStructures.Geometry;

[Flags]
public enum Edge : short
{
    None = 0,
    Top = 1,
    Right = 2,
    Left = 4,
    Bottom = 8,
}

public static class EdgeExtensions
{
    public static Edge GetOpposite(this Edge edge)
    {
        switch (edge)
        {
            case Edge.Top: return Edge.Bottom;
            case Edge.Bottom: return Edge.Top;
            case Edge.Left: return Edge.Right;
            case Edge.Right: return Edge.Left;
        }

        return Edge.None;
    }

    public static Axis GetAxis(this Edge edge)
    {
        switch (edge)
        {
            case Edge.Bottom:
            case Edge.Top:
                return Axis.Row;
            case Edge.Left:
            case Edge.Right:
                return Axis.Col;
            default:
                return Axis.None;
        }
    }
}