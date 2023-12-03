using BlazorDatasheet.Formula.Core;
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
                ParameterType.NumberSequence,
                ParameterRequirement.Required,
                isRepeating: true)
        };
    }

    public object? Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var sum = 0d;
        foreach (var arg in args)
        {
            var seq = arg.GetValue<CellValue[]>()!;
            foreach (var item in seq)
                if (item.IsError())
                    return item.Data;
                else
                    sum += item.GetValue<double>();
        }

        return sum;
    }

    public bool AcceptsErrors => true;
}