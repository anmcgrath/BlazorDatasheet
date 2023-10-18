using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Formula.Core.CoreFunctions;

public class SinFunction : CallableFunctionDefinition
{
    public SinFunction() : base(new[]
    {
        new Parameter("x", ParameterType.Number, ParameterRequirement.Required)
    })
    {
    }

    public override object Call(IEnumerable<object> arguments)
    {
        return Math.Sin((double)arguments.First());
    }

    public override Type ReturnType => typeof(double);
    public override bool AcceptsErrors => false;
}