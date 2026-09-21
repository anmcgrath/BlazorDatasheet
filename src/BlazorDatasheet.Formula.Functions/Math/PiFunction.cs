using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class PiFunction
{
    private static readonly ParameterDefinition[] Parameters = [];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "PI",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the value of pi.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(System.Math.PI);
    }
}
