using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class RandFunction
{
    private static readonly ParameterDefinition[] Parameters = [];
    private static readonly Random Random = new();

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "RAND",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: true);

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(Random.NextDouble());
    }
}
