using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public class PowerFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("number",
                ParameterType.Number,
                ParameterRequirement.Required,
                isRepeating: false),
            new ParameterDefinition("exponent",
                ParameterType.Number,
                ParameterRequirement.Required,
                isRepeating: false)
        };
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var number = args[0].GetValue<double>();
        var exponent = args[1].GetValue<double>();

        return CellValue.Number(System.Math.Pow(number, exponent));
    }

    public bool AcceptsErrors => false;
    public bool IsVolatile => false;
}