namespace BlazorDatasheet.Formula.Core;

public class ParameterDefinition
{
    public string Name { get; init; }
    public ParameterType Type { get; init; }
    public bool IsRepeating { get; init; }
    public ParameterRequirement Requirement { get; init; }
    public ParameterShape Shape { get; init; }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterRequirement requirement = ParameterRequirement.Required,
        bool isRepeating = false,
        ParameterShape shape = ParameterShape.Scalar)
    {
        Name = name;
        Type = type;
        IsRepeating = isRepeating;
        Requirement = requirement;
        Shape = shape;
    }
}
