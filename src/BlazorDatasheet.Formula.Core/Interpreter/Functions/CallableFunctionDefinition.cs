namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public abstract class CallableFunctionDefinition
{
    public const int MAX_ARGS = 128;
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
        ValidateOrThrow(parameters);

        MinArity = parameters.Count(x => x.Requirement == ParameterRequirement.Required);
        MaxArity = parameters.Last().IsRepeating ? MAX_ARGS : parameters.Length;
    }

    private void ValidateOrThrow(Parameter[] parameters)
    {
        if (!parameters.Any())
            return;

        var hasOptional = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (hasOptional && parameters[i].Requirement == ParameterRequirement.Required)
                throw new InvalidFunctionDefinitionException("Required parameters cannot be defined after optional.",
                                                             parameters[i].ParameterName);
            if (parameters[i].IsRepeating && i != parameters.Length - 1)
                throw new InvalidFunctionDefinitionException(
                    "Repeating parameters must be defined as the last parameter", parameters[i].ParameterName);

            hasOptional = hasOptional ||
                          parameters[i].Requirement == ParameterRequirement.Optional ||
                          parameters[i].IsRepeating;
        }
    }
}