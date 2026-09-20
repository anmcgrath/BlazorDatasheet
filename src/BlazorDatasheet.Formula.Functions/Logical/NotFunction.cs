using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public static class NotFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("value", ParameterType.Logical, ParameterRequirement.Required,
            description: "The logical value to reverse.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "NOT",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the opposite of a logical value.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        return CellValue.Logical(!args[0].GetValue<bool>());
    }
}
