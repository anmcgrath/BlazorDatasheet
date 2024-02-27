using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public class SinFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("x",
                ParameterType.Number,
                ParameterRequirement.Required,
                isRepeating: false)
        };
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var val = args[0];
        if (val.ValueType == CellValueType.Error)
            return val;

        if (val.IsEmpty)
            return CellValue.Number(0); // Math.Sin(0);

        if (val.ValueType != CellValueType.Number)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Number(System.Math.Sin((double)val.Data!));
    }

    public bool AcceptsErrors => false;
}