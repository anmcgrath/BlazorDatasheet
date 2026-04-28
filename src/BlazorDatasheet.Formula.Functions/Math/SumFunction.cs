using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class SumFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new(
            "number",
            ParameterType.NumberSequence,
            ParameterRequirement.Required,
            isRepeating: true,
            shape: ParameterShape.ScalarOrArray)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SUM",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

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

                sum += item.GetValue<double>();
            }
        }

        return CellValue.Number(sum);
    }
}
