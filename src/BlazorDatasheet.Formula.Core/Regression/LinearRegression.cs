using System.Diagnostics;

namespace BlazorDatasheet.Formula.Core.Regression;

public class LinearRegression
{
    public LinearFunction Calculate(List<double> x, List<double> y)
    {
        Debug.Assert(x.Count == y.Count);
        Debug.Assert(x.Count > 1 && y.Count > 1);
        var avgX = x.Sum() / x.Count;
        var avY = y.Sum() / y.Count;
        var top = x.Zip(y).Select((x) => (x.First - avgX) * (x.Second - avY)).Sum();
        var bot = x.Select(x => Math.Pow(x - avgX, 2)).Sum();
        var m = bot == 0 ? 0 : top / bot;
        var c = avY - m * avgX;
        return new LinearFunction(m, c);
    }
}