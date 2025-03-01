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
        var number = args[0];
        var exponent = args[1];

        if (number.ValueType != CellValueType.Number)
            return CellValue.Error(ErrorType.Value);
        
        if (exponent.ValueType != CellValueType.Number)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Number(System.Math.Pow((double)number.Data!, (double)exponent.Data!));
    }

    public bool AcceptsErrors => false;
    public bool IsVolatile => false;
}