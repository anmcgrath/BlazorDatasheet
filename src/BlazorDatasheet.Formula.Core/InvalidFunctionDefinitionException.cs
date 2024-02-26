namespace BlazorDatasheet.Formula.Core;

public class InvalidFunctionDefinitionException : Exception
{
    public string ParameterMessage { get; }
    public string ParameterName { get; }

    public InvalidFunctionDefinitionException(
        string parameterMessage,
        string parameterName)
    {
        ParameterMessage = parameterMessage;
        ParameterName = parameterName;
    }
}