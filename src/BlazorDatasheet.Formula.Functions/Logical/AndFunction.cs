using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public static class AndFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new(
            name: "logical",
            type: ParameterType.LogicalSequence,
            requirement: ParameterRequirement.Required,
            isRepeating: true,
            shape: ParameterShape.ScalarOrArray)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "AND",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var isTrue = true;
        foreach (var arg in args)
        {
            var seq = arg.GetValue<CellValue[]>()!;
            if (seq.Length == 0)
                return CellValue.Error(ErrorType.Value);

            foreach (var cv in seq)
            {
                if (cv.IsError())
                    return cv;

                isTrue &= cv.GetValue<bool>();
            }
        }

        return CellValue.Logical(isTrue);
    }
}
