using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatashet.Formula.Functions.Logical;

public class AndFunction : CallableFunctionDefinition
{
    public AndFunction() : base(new[]
    {
        new Parameter("logical1", ParameterType.Logical, ParameterRequirement.Required),
        new Parameter("logical2", ParameterType.Logical, ParameterRequirement.Optional, isRepeating: true)
    })
    {
    }

    public override object Call(List<object> arguments)
    {
        var logical1 = (bool)arguments.First();
        var rest = arguments.Skip(1).Take(arguments.Count - 1).Cast<bool>();
        return logical1 && rest.All(x => x);
    }

    public override Type ReturnType => typeof(bool);
    public override bool AcceptsErrors => false;
}