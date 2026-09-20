using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public static class IfFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("logical1", ParameterType.Logical, ParameterRequirement.Required,
            description: "The condition to test."),
        new("val_if_true", ParameterType.Any, ParameterRequirement.Optional, shape: ParameterShape.ScalarOrArray,
            description: "The value returned when the condition is true."),
        new("val_if_false", ParameterType.Any, ParameterRequirement.Optional, shape: ParameterShape.ScalarOrArray,
            description: "The value returned when the condition is false.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "IF",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: true,
        isVolatile: false,
        returnShape: ReturnShape.Scalar,
        description: "Returns one value if a condition is true and another if it is false.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        if (args[0].IsError())
            return args[0];

        var isTrue = args[0].GetValue<bool>();
        if (args.Length > 1 && isTrue)
            return args[1];

        if (args.Length > 2 && !isTrue)
            return args[2];

        return CellValue.Logical(isTrue);
    }
}
