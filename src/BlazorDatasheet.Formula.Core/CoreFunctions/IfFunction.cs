using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Formula.Core.CoreFunctions;

public class IfFunction : CallableFunctionDefinition
{
    public IfFunction() : base(new[]
    {
        new Parameter("logical_test", ParameterType.Logical, ParameterRequirement.Required),
        new Parameter("value_if_true", ParameterType.Any, ParameterRequirement.Required),
        new Parameter("value_if_false", ParameterType.Any, ParameterRequirement.Optional)
    })
    {
    }

    public override object Call(IEnumerable<object> arguments)
    {
        var args = arguments.ToList();
        var isTrue = (bool)args[0];
        if (args.Count > 1 && isTrue)
            return args[1];
        if (args.Count > 2 && !isTrue)
            return args[2];
        return isTrue;
    }

    public override Type ReturnType { get; }
    public override bool AcceptsErrors => false;
}