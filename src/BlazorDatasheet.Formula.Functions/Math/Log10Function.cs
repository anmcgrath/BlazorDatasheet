using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class Log10Function
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to find the logarithm of. Must be greater than zero.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "LOG10",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the logarithm of a number, base 10.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var number = args[0].GetValue<double>();
        if (number <= 0)
            return CellValue.Error(ErrorType.Num);

        return CellValue.Number(System.Math.Log10(number));
    }
}
