namespace BlazorDatasheet.Formula.Core;

public class FunctionCallMetaData
{
    internal FunctionCallMetaData(ParameterDefinition[] parameterDefinitions)
    {
        ParameterDefinitions = parameterDefinitions;
    }

    public ParameterDefinition[] ParameterDefinitions { get; }
}