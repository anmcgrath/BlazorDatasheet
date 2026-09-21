using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class LogFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to find the logarithm of. Must be greater than zero."),
        new("base", ParameterType.Number, ParameterRequirement.Optional,
            description: "The base of the logarithm. 10 by default.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "LOG",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the logarithm of a number to the given base.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var number = args[0].GetValue<double>();
        var logBase = args.Length > 1 ? args[1].GetValue<double>() : 10d;

        if (number <= 0 || logBase <= 0)
            return CellValue.Error(ErrorType.Num);

        // A base of one has no logarithm - excel reports this as a division by zero.
        if (logBase == 1)
            return CellValue.Error(ErrorType.Div0);

        return CellValue.Number(System.Math.Log(number, logBase));
    }
}
