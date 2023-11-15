using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatashet.Formula.Functions.Math;

public class AverageFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition(
                name: "number1",
                dimensionality: ParameterDimensionality.Range,
                isRepeating: true,
                requirement: ParameterRequirement.Required,
                type: ParameterType.Number
            )
        };
    }

    public object Call(FuncArg[] args)
    {
        var vals = args[0].Flatten().Where(x => x.ValueType == CellValueType.Number).Select(x => x.Data).Cast<double>()
            .ToArray();
        if (vals.Length == 0)
            return new FormulaError(ErrorType.Div0);

        return vals.Average();
    }

    public bool AcceptsErrors => false;
}