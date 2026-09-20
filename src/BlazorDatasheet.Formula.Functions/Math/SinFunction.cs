using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class SinFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("x", ParameterType.Number, ParameterRequirement.Required,
            description: "The angle, in radians.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SIN",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the sine of an angle.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(System.Math.Sin(args[0].GetValue<double>()));
    }
}
