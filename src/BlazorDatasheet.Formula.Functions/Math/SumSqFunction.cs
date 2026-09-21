using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class SumSqFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new(
            "number",
            ParameterType.NumberSequence,
            ParameterRequirement.Required,
            isRepeating: true,
            shape: ParameterShape.ScalarOrArray,
            description: "A number or range to square and add.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SUMSQ",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the sum of the squares of a series of numbers.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var sum = 0d;
        foreach (var arg in args)
        {
            var seq = (CellValue[])arg.Data!;
            foreach (var item in seq)
            {
                if (item.IsError())
                    return item;

                var value = item.GetValue<double>();
                sum += value * value;
            }
        }

        return CellValue.Number(sum);
    }
}
