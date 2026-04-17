using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public static class IfFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("logical1", ParameterType.Logical, ParameterRequirement.Required),
        new("val_if_true", ParameterType.Any, ParameterRequirement.Optional, shape: ParameterShape.ScalarOrArray),
        new("val_if_false", ParameterType.Any, ParameterRequirement.Optional, shape: ParameterShape.ScalarOrArray)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "IF",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: true,
        isVolatile: false,
        returnShape: ReturnShape.Scalar);

    private static CellValue Evaluate(CellValue[] args, FunctionCallMetaData metaData)
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
