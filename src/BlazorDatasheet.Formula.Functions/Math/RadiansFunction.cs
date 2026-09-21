using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class RadiansFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("angle", ParameterType.Number, ParameterRequirement.Required,
            description: "The angle, in degrees, to convert to radians.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "RADIANS",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Converts an angle in degrees to radians.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(args[0].GetValue<double>() * System.Math.PI / 180);
    }
}
