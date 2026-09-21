using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class DegreesFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("angle", ParameterType.Number, ParameterRequirement.Required,
            description: "The angle, in radians, to convert to degrees.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "DEGREES",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Converts an angle in radians to degrees.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(args[0].GetValue<double>() * 180 / System.Math.PI);
    }
}
