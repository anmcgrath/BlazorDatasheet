using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Formula.Functions.Logical;

public class OrFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition(
                name: "logical",
                ParameterType.Logical,
                ParameterDimensionality.Range,
                ParameterRequirement.Required,
                isRepeating: true
            )
        };
    }

    public object Call(FuncArg[] args)
    {
        var vals = args.First().Flatten();
        return vals.Select(x => x.GetValue<bool>()).Any(x => x == true);
    }

    public bool AcceptsErrors => false;
}