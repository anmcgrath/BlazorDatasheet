using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class ModFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to divide."),
        new("divisor", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to divide by. Must not be zero.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "MOD",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the remainder after dividing a number by a divisor. " +
                     "The result has the same sign as the divisor.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var divisor = args[1].GetValue<double>();
        if (divisor == 0)
            return CellValue.Error(ErrorType.Div0);

        var number = args[0].GetValue<double>();

        // Unlike the c# % operator, the result takes the sign of the divisor, so that MOD(-3, 2) = 1.
        return CellValue.Number(number - divisor * System.Math.Floor(number / divisor));
    }
}
