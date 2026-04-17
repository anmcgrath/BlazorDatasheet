using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public static class NotFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("value", ParameterType.Logical, ParameterRequirement.Required)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "NOT",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(CellValue[] args, FunctionCallMetaData metaData)
    {
        return CellValue.Logical(!args[0].GetValue<bool>());
    }
}
