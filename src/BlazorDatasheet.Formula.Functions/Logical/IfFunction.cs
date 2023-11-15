using BlazorDatasheet.Formula.Core.Interpreter.Functions;

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
                ParameterDimensionality.Scalar,
                ParameterRequirement.Required
            ),
            new ParameterDefinition(
                "val_if_true",
                ParameterType.Any,
                ParameterDimensionality.Scalar,
                ParameterRequirement.Optional
            ),
            new ParameterDefinition(
                "val_if_false",
                ParameterType.Any,
                ParameterDimensionality.Scalar,
                ParameterRequirement.Optional
            ),
        };
    }

    public object Call(FuncArg[] args)
    {
        var isTrue = args.First().AsScalar().GetValue<bool>();
        if (args.Length > 1 && isTrue)
        {
            Console.WriteLine(args[1].Value);
            return args[1].AsScalar().Data;
        }

        if (args.Length > 2 && !isTrue)
            return args[2].AsScalar().Data;
        return isTrue;
    }

    public bool AcceptsErrors => false;
}