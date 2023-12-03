using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Functions.Logical;

public class OrFunction : ISheetFunction
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
        var isTrue = false;
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
                    isTrue |= cv.GetValue<bool>();
            }
        }

        return isTrue;
    }

    public bool AcceptsErrors => false;
}