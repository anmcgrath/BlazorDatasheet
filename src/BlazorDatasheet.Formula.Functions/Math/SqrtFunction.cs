using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class SqrtFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to take the square root of. Must not be negative.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SQRT",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the positive square root of a number.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var number = args[0].GetValue<double>();
        if (number < 0)
            return CellValue.Error(ErrorType.Num);

        return CellValue.Number(System.Math.Sqrt(number));
    }
}
