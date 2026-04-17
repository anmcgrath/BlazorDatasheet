using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class AverageFunction
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
        name: "AVERAGE",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(CellValue[] args, FunctionCallMetaData metaData)
    {
        var sum = 0d;
        var count = 0d;
        foreach (var arg in args)
        {
            var seq = arg.GetValue<CellValue[]>()!;
            foreach (var item in seq)
            {
                if (item.IsError())
                    return item;

                sum += item.GetValue<double>();
                count++;
            }
        }

        return CellValue.Number(sum / count);
    }
}
