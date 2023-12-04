using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatashet.Formula.Functions.Math;

public class InterceptFunction : ISheetFunction
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
        var ys = args[0].GetValue<CellValue[][]>()!;
        var xs = args[1].GetValue<CellValue[][]>()!;

        if (!xs.Any() || !ys.Any())
            return new FormulaError(ErrorType.Na, "Empty array");

        if (xs.Length != ys.Length)
            return new FormulaError(ErrorType.Na, "X and Y number of rows must be the same");

        if (xs[0].Length != ys[0].Length)
            return new FormulaError(ErrorType.Na, "X and Y number of columns must be the same");

        return 0;
    }

    public bool AcceptsErrors => false;
}