using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

public static class TransposeFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("array", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "The range or array to transpose.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "TRANSPOSE",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        returnShape: ReturnShape.Array,
        description: "Returns the rows of a range or array as columns, and its columns as rows.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Array(ArrayFunctionHelpers.Transpose(ArrayFunctionHelpers.Rows(args[0])));
    }
}
