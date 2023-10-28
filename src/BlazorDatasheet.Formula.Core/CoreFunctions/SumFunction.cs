using System.Diagnostics;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Formula.Core.CoreFunctions;

public class SumFunction : CallableFunctionDefinition
{
    public SumFunction() : base(
        new Parameter("number", ParameterType.NumberSequence, ParameterRequirement.Required, isRepeating: false),
        new Parameter("numbers", ParameterType.NumberSequence, ParameterRequirement.Optional, isRepeating: true))
    {
    }

    public override object Call(IEnumerable<object> arguments)
    {
        var args = arguments.Cast<IEnumerable<double>>()
            .ToList();

        return args.Select(x => x.Sum()).Sum();
        return 0;
    }

    public override Type ReturnType => typeof(double);
    public override bool AcceptsErrors => false;
}