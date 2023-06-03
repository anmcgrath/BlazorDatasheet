namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class Parameter
{
    public string ParameterName { get; }
    public ParameterType ParameterType { get; }
    public ParameterRequirement Requirement { get; }
    public bool IsRepeating { get; }

    public Parameter(string parameterName,
        ParameterType parameterType,
        ParameterRequirement requirement,
        bool isRepeating = false)
    {
        ParameterName = parameterName;
        ParameterType = parameterType;
        Requirement = requirement;
        IsRepeating = isRepeating;
    }
}