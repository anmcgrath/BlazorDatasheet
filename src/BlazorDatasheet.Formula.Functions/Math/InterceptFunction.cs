using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Regression;

namespace BlazorDatashet.Formula.Functions.Math;

public static class InterceptFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("known_ys", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array),
        new("known_xs", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "INTERCEPT",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var allY = args[0].GetValue<CellValue[][]>()!;
        var allX = args[1].GetValue<CellValue[][]>()!;

        if (allX.Length == 0 || allY.Length == 0)
            return CellValue.Error(ErrorType.Na, "Empty array");

        if (allX.Length != allY.Length)
            return CellValue.Error(ErrorType.Na, "X and Y number of rows must be the same");

        if (allX[0].Length != allY[0].Length)
            return CellValue.Error(ErrorType.Na, "X and Y number of columns must be the same");

        var x = new List<double>();
        var y = new List<double>();

        for (int row = 0; row < allX.Length; row++)
        {
            for (int col = 0; col < allX[row].Length; col++)
            {
                if (allX[row][col].ValueType == CellValueType.Number &&
                    allY[row][col].ValueType == CellValueType.Number)
                {
                    x.Add(allX[row][col].GetValue<double>());
                    y.Add(allY[row][col].GetValue<double>());
                }
            }
        }

        if (x.Count <= 1)
            return CellValue.Error(ErrorType.Div0);

        var regression = new LinearRegression();
        var fun = regression.Calculate(x, y);

        return CellValue.Number(fun.YIntercept);
    }
}
