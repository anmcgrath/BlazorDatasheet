using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Logical;

public class AndFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition(
                name: "logical",
                type: ParameterType.LogicalSequence,
                requirement: ParameterRequirement.Required,
                isRepeating: true
            )
        };
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var isTrue = true;
        foreach (var arg in args)
        {
            var seq = arg.GetValue<CellValue[]>()!;
            if (seq.Length == 0)
                return CellValue.Error(ErrorType.Value);
            foreach (var cv in seq)
            {
                if (cv.IsError())
                    return cv;
                else
                    isTrue &= cv.GetValue<bool>();
            }
        }

        return CellValue.Logical(isTrue);
    }

    public bool AcceptsErrors => false;
}