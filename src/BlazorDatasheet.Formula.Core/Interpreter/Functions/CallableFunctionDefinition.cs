namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public abstract class CallableFunctionDefinition
{
    public const int MaxArgs = 128;
    private Parameter[] _parameters;
    public IReadOnlyList<Parameter> Parameters => _parameters;
    public abstract object Call(List<object> arguments);
    public abstract Type ReturnType { get; }
    public abstract bool AcceptsErrors { get; }
    internal int MinArity { get; }
    internal int MaxArity { get; }

    protected CallableFunctionDefinition(params Parameter[] parameters)
    {
        _parameters = parameters;

        MinArity = parameters.Count(x => x.Requirement == ParameterRequirement.Required);
        MaxArity = parameters.Last().IsRepeating ? MaxArgs : parameters.Length;
    }
}