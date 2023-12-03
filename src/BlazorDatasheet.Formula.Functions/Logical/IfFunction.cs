using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Functions.Logical;

public class IfFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition(
                "logical1",
                ParameterType.Logical,
                ParameterRequirement.Required
            ),
            new ParameterDefinition(
                "val_if_true",
                ParameterType.Any,
                ParameterRequirement.Optional
            ),
            new ParameterDefinition(
                "val_if_false",
                ParameterType.Any,
                ParameterRequirement.Optional
            ),
        };
    }

    public object? Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        if (args[0].IsError())
            return args[0].Data;

        var isTrue = args.First().GetValue<bool>();
        if (args.Length > 1 && isTrue)
        {
            return args[1].Data;
        }

        if (args.Length > 2 && !isTrue)
            return args[2].Data;
        return isTrue;
    }

    public bool AcceptsErrors => false;
}