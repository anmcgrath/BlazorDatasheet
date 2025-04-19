using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public class NotFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new ParameterDefinition[]
        {
            new("value", ParameterType.Logical, ParameterRequirement.Required)
        };
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var val = args[0].GetValue<bool>();
        return CellValue.Logical(!val);
    }

    public bool AcceptsErrors => false;
    public bool IsVolatile => false;
}