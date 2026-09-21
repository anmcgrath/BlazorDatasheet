using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class MaxFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new(
            "number",
            ParameterType.NumberSequence,
            ParameterRequirement.Required,
            isRepeating: true,
            shape: ParameterShape.ScalarOrArray,
            description: "A number or range to find the maximum of.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "MAX",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the largest of a series of numbers. Text and logical values in ranges are ignored.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var max = 0d;
        var any = false;

        foreach (var arg in args)
        {
            var seq = (CellValue[])arg.Data!;
            foreach (var item in seq)
            {
                if (item.IsError())
                    return item;

                var value = item.GetValue<double>();
                if (!any || value > max)
                    max = value;

                any = true;
            }
        }

        // Excel returns zero when there are no numbers to compare.
        return CellValue.Number(any ? max : 0);
    }
}
