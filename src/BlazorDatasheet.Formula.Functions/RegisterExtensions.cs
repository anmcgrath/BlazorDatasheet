using BlazorDatasheet.Formula.Core;
using BlazorDatashet.Formula.Functions.Logical;
using BlazorDatashet.Formula.Functions.Math;

namespace BlazorDatashet.Formula.Functions;

public static class RegisterExtensions
{
    public static void RegisterLogicalFunctions(this IEnvironment e)
    {

    }

    public static void RegisterMathFunctions(this IEnvironment e)
    {
        e.SetFunction("AVERAGE", new AverageFunction());
        e.SetFunction("SUM", new SumFunction());
        e.SetFunction("SIN", new SinFunction());
    }
}