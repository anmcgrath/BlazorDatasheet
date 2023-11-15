namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class ParameterDefinition
{
    public string Name { get; init; }
    public ParameterType Type { get; init; }
    public ParameterDimensionality Dimensionality { get; init; }
    public bool IsRepeating { get; init; }
    public ParameterRequirement Requirement { get; init; }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterDimensionality dimensionality,
        ParameterRequirement requirement = ParameterRequirement.Required,
        bool isRepeating = false)
    {
        Name = name;
        Type = type;
        Dimensionality = dimensionality;
        IsRepeating = isRepeating;
        Requirement = requirement;
    }

}