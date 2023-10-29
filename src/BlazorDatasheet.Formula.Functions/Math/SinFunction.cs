using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatashet.Formula.Functions.Math;

public class SinFunction : CallableFunctionDefinition
{
    public SinFunction() : base(new[]
    {
        new Parameter("x", ParameterType.Number, ParameterRequirement.Required)
    })
    {
    }

    public override object Call(List<object> arguments)
    {
        return System.Math.Sin((double)arguments[0]);
    }

    public override Type ReturnType => typeof(double);
    public override bool AcceptsErrors => false;
}