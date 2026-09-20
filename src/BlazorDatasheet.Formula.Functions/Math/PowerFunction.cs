using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class PowerFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to raise."),
        new("exponent", ParameterType.Number, ParameterRequirement.Required,
            description: "The power to raise the number to.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "POW",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns a number raised to a power.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(System.Math.Pow(args[0].GetValue<double>(), args[1].GetValue<double>()));
    }
}
