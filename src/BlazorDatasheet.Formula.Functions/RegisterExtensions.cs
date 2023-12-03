using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Functions.Logical;
using BlazorDatashet.Formula.Functions.Math;

namespace BlazorDatashet.Formula.Functions;

public static class RegisterExtensions
{
    public static void RegisterLogicalFunctions(this IEnvironment e)
    {
        e.RegisterFunction("AND", new AndFunction());
        e.RegisterFunction("IF", new IfFunction());
        e.RegisterFunction("OR", new OrFunction());
    }

    public static void RegisterMathFunctions(this IEnvironment e)
    {
        e.RegisterFunction("AVERAGE", new AverageFunction());
        e.RegisterFunction("SUM", new SumFunction());
        e.RegisterFunction("SIN", new SinFunction());
    }
}