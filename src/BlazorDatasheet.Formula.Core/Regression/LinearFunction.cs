namespace BlazorDatasheet.Formula.Core.Regression;

public class LinearFunction
{
    public double Gradient { get; init; }
    public double YIntercept { get; init; }

    public LinearFunction(double gradient, double yIntercept)
    {
        Gradient = gradient;
        YIntercept = yIntercept;
    }

    public double ComputeY(double x)
    {
        return Gradient * x + YIntercept;
    }

    public double ComputeX(double y)
    {
        if (Gradient == 0)
            return double.NaN;
        return (y - YIntercept) / Gradient;
    }
}