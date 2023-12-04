using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;
using BlazorDatasheet.Formula.Core.Regression;

namespace BlazorDatashet.Formula.Functions.Math;

public class SlopeFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("known_ys", ParameterType.Array, ParameterRequirement.Required),
            new ParameterDefinition("known_xs", ParameterType.Array, ParameterRequirement.Required)
        };
    }

    public object? Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var allY = args[0].GetValue<CellValue[][]>()!;
        var allX = args[1].GetValue<CellValue[][]>()!;

        if (!allX.Any() || !allY.Any())
            return new FormulaError(ErrorType.Na, "Empty array");

        if (allX.Length != allY.Length)
            return new FormulaError(ErrorType.Na, "X and Y number of rows must be the same");

        if (allX[0].Length != allY[0].Length)
            return new FormulaError(ErrorType.Na, "X and Y number of columns must be the same");

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
            return new FormulaError(ErrorType.Div0);

        var regression = new LinearRegression();
        var fun = regression.Calculate(x, y);

        return fun.Gradient;
    }

    public bool AcceptsErrors => false;
}