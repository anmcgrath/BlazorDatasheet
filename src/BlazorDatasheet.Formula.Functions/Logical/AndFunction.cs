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

    public object? Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var isTrue = true;
        foreach (var arg in args)
        {
            var seq = arg.GetValue<CellValue[]>()!;
            if (seq.Length == 0)
                return new FormulaError(ErrorType.Value);
            foreach (var cv in seq)
            {
                if (cv.IsError())
                    return cv.Data!;
                else
                    isTrue &= cv.GetValue<bool>();
            }
        }

        return isTrue;
    }

    public bool AcceptsErrors => false;
}