using BlazorDatasheet.Formula.Core;
using BlazorDatashet.Formula.Functions.Logical;
using BlazorDatashet.Formula.Functions.Math;

namespace BlazorDatashet.Formula.Functions;

public static class RegisterExtensions
{
    public static void RegisterLogicalFunctions(this IEnvironment e)
    {
        e.SetFunction("IF", new IfFunction());
        e.SetFunction("AND", new AndFunction());
    }

    public static void RegisterMathFunctions(this IEnvironment e)
    {
        e.SetFunction("SIN", new SinFunction());
        e.SetFunction("SUM", new SumFunction());
    }
}