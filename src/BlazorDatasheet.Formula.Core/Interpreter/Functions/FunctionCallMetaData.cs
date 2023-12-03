namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class FunctionCallMetaData
{
    internal FunctionCallMetaData(ParameterDefinition[] parameterDefinitions)
    {
        ParameterDefinitions = parameterDefinitions;
    }

    public ParameterDefinition[] ParameterDefinitions { get; }
}