using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public static class RoundFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("number", ParameterType.Number, ParameterRequirement.Required,
            description: "The number to round."),
        new("num_digits", ParameterType.Number, ParameterRequirement.Optional,
            description: "The number of decimal places to round to. Zero by default. " +
                         "Negative values round to the left of the decimal point.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "ROUND",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Rounds a number to a given number of decimal places.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var number = args[0].GetValue<double>();
        var digits = args.Length > 1 ? System.Math.Truncate(args[1].GetValue<double>()) : 0d;

        if (double.IsNaN(number) || double.IsInfinity(number))
            return CellValue.Number(number);

        if (digits >= 0)
        {
            // A double has at most 15 significant decimal digits, so asking for more fractional digits
            // cannot change its value. Decimal rounding avoids binary midpoint errors such as 1.005 * 100.
            if (digits > 15 || number is > (double)decimal.MaxValue or < (double)decimal.MinValue)
                return CellValue.Number(number);

            var rounded = decimal.Round((decimal)number, (int)digits, MidpointRounding.AwayFromZero);
            return CellValue.Number((double)rounded);
        }

        // Divide before rounding for negative digit counts. This avoids underflowing a factor such as
        // 10^-400 and then producing NaN by dividing zero by zero.
        var factor = System.Math.Pow(10, -digits);
        if (double.IsInfinity(factor))
            return CellValue.Number(0);

        return CellValue.Number(System.Math.Round(number / factor, MidpointRounding.AwayFromZero) * factor);
    }
}
