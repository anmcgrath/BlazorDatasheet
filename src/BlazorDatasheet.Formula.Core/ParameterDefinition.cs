namespace BlazorDatasheet.Formula.Core;

public class ParameterDefinition
{
    public string Name { get; init; }
    public ParameterType Type { get; init; }
    public bool IsRepeating { get; init; }
    public ParameterRequirement Requirement { get; init; }
    public ParameterShape Shape { get; init; }

    /// <summary>
    /// Describes the parameter to the user, in the formula hint box.
    /// </summary>
    public string? Description { get; init; }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterRequirement requirement = ParameterRequirement.Required,
        bool isRepeating = false,
        ParameterShape shape = ParameterShape.Scalar,
        string? description = null)
    {
        Name = name;
        Type = type;
        IsRepeating = isRepeating;
        Requirement = requirement;
        Shape = shape;
        Description = description;
    }
}
