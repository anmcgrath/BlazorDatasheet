using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class SinFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("x", ParameterType.Number, ParameterRequirement.Required)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SIN",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(CellValue[] args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(System.Math.Sin(args[0].GetValue<double>()));
    }
}
