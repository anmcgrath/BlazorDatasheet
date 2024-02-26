using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public class AverageFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition(
                "number",
                ParameterType.NumberSequence,
                ParameterRequirement.Required,
                isRepeating: true
            )
        };
    }

    public object? Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var sum = 0d;
        var count = 0d;
        foreach (var arg in args)
        {
            var seq = arg.GetValue<CellValue[]>()!;
            foreach (var item in seq)
                if (item.IsError())
                    return item.Data!;
                else
                {
                    sum += item.GetValue<double>();
                    count++;
                }
        }

        return sum / count;
    }

    public bool AcceptsErrors => false;
}