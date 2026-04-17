namespace BlazorDatasheet.Formula.Core;

public sealed class FunctionRegistryBuilder
{
    private readonly Dictionary<string, FunctionDescriptor> _functions = new(StringComparer.OrdinalIgnoreCase);
    private bool _isBuilt;

    public FunctionRegistryBuilder Add(FunctionDescriptor descriptor)
    {
        if (_isBuilt)
            throw new InvalidOperationException("Cannot add functions after registry has been built.");

        if (!_functions.TryAdd(descriptor.Name, descriptor))
            throw new InvalidOperationException($"Function '{descriptor.Name}' has already been registered.");

        return this;
    }

    public FunctionRegistry Build()
    {
        if (_isBuilt)
            throw new InvalidOperationException("Registry has already been built.");

        _isBuilt = true;
        return new FunctionRegistry(_functions);
    }
}
