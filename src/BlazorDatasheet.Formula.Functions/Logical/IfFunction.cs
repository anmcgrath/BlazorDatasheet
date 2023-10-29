using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatashet.Formula.Functions.Logical;

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

    public override object Call(List<object> arguments)
    {
        var isTrue = (bool)arguments[0];
        if (arguments.Count > 1 && isTrue)
            return arguments[1];
        if (arguments.Count > 2 && !isTrue)
            return arguments[2];
        return isTrue;
    }

    public override Type ReturnType { get; }
    public override bool AcceptsErrors => false;
}