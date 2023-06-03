namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class InvalidFunctionDefinitionException : Exception
{
    public string RequiredParametersCannotBeDefinedAfterOptional { get; }
    public string ParameterName { get; }

    public InvalidFunctionDefinitionException(
        string requiredParametersCannotBeDefinedAfterOptional,
        string parameterName)
    {
        RequiredParametersCannotBeDefinedAfterOptional = requiredParametersCannotBeDefinedAfterOptional;
        ParameterName = parameterName;
    }
}