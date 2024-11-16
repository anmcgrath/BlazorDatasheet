namespace BlazorDatasheet.DataStructures.Geometry;

public class Rect
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


    public override string ToString()
    {
        return $"X: {X}, Y: {Y}, Width: {Width}, Height: {Height}";
    }
}