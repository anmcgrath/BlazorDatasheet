using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class PowerFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required),
        new("exponent", ParameterType.Number, ParameterRequirement.Required)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "POW",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(CellValue[] args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(System.Math.Pow(args[0].GetValue<double>(), args[1].GetValue<double>()));
    }
}
