namespace BlazorDatasheet.Formula.Core;

public delegate CellValue FunctionInvoker(ReadOnlySpan<CellValue> args, FunctionCallMetaData callMetaData);

public sealed class FunctionDescriptor
{
    private readonly FunctionInvoker _invoker;

    public string Name { get; }
    public ParameterDefinition[] ParameterDefinitions { get; }
    public int MinArity { get; }
    public int MaxArity { get; }
    public bool AcceptsErrors { get; }
    public bool IsVolatile { get; }
    public ReturnShape ReturnShape { get; }

    public FunctionDescriptor(
        string name,
        ParameterDefinition[] parameterDefinitions,
        FunctionInvoker invoker,
        bool acceptsErrors = false,
        bool isVolatile = false,
        ReturnShape returnShape = ReturnShape.Scalar)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Function name cannot be null or empty", nameof(name));

        Name = name;
        ParameterDefinitions = parameterDefinitions ?? throw new ArgumentNullException(nameof(parameterDefinitions));
        _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        AcceptsErrors = acceptsErrors;
        IsVolatile = isVolatile;
        ReturnShape = returnShape;

        var validator = new FunctionParameterValidator();
        validator.ValidateOrThrow(ParameterDefinitions);

        MinArity = ComputeMinArity(ParameterDefinitions);
        MaxArity = ComputeMaxArity(ParameterDefinitions);
    }

    public CellValue Invoke(ReadOnlySpan<CellValue> args, FunctionCallMetaData callMetaData)
    {
        return _invoker(args, callMetaData);
    }

    private static int ComputeMinArity(ParameterDefinition[] parameterDefinitions)
    {
        var min = 0;
        for (int i = 0; i < parameterDefinitions.Length; i++)
        {
            if (parameterDefinitions[i].Requirement == ParameterRequirement.Required)
                min++;
        }

        return min;
    }

    private static int ComputeMaxArity(ParameterDefinition[] parameterDefinitions)
    {
        if (parameterDefinitions.Length == 0)
            return 0;

        return parameterDefinitions[^1].IsRepeating ? 128 : parameterDefinitions.Length;
    }
}
