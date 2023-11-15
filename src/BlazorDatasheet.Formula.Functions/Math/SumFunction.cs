using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatashet.Formula.Functions.Math;

public class SumFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition(
                "number",
                ParameterType.Number,
                ParameterDimensionality.Range,
                ParameterRequirement.Required,
                isRepeating: true)
        };
    }

    public object Call(FuncArg[] args)
    {
        var nums = args.First().Flatten();
        var total = 0d;
        foreach (var val in nums)
        {
            if (val.ValueType == CellValueType.Error)
                return val.Data;
            else if (val.ValueType == CellValueType.Number)
                total += val.GetValue<double>();
        }

        return total;
    }

    public bool AcceptsErrors { get; }
}