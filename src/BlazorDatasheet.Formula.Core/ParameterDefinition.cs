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
        ParameterShape shape = ParameterShape.Scalar)
        : this(name, type, requirement, isRepeating, shape, null)
    {
    }

    public ParameterDefinition(string name, ParameterType type, string? description)
        : this(name, type, ParameterRequirement.Required, false, ParameterShape.Scalar, description)
    {
    }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterRequirement requirement,
        string? description)
        : this(name, type, requirement, false, ParameterShape.Scalar, description)
    {
    }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterRequirement requirement,
        ParameterShape shape,
        string? description)
        : this(name, type, requirement, false, shape, description)
    {
    }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterRequirement requirement,
        bool isRepeating,
        string? description)
        : this(name, type, requirement, isRepeating, ParameterShape.Scalar, description)
    {
    }

    public ParameterDefinition(string name,
        ParameterType type,
        ParameterRequirement requirement,
        bool isRepeating,
        ParameterShape shape,
        string? description)
    {
        Name = name;
        Type = type;
        IsRepeating = isRepeating;
        Requirement = requirement;
        Shape = shape;
        Description = description;
    }
}
