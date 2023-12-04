namespace BlazorDatasheet.Formula.Core.Regression;

public class LinearFunction
{
    public double M { get; init; }
    public double C { get; init; }

    public LinearFunction(double m, double c)
    {
        M = m;
        C = c;
    }

    public double ComputeY(double x)
    {
        return M * x + C;
    }

    public double ComputeX(double y)
    {
        if (M == 0)
            return double.NaN;
        return (y - C) / M;
    }
}