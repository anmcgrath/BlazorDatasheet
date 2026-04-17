using System.Diagnostics.CodeAnalysis;

namespace BlazorDatasheet.Formula.Core;

public sealed class FunctionRegistry
{
    private readonly Dictionary<string, FunctionDescriptor> _functions;
    private readonly string[] _sortedNames;

    internal FunctionRegistry(IDictionary<string, FunctionDescriptor> functions)
    {
        _functions = new Dictionary<string, FunctionDescriptor>(functions, StringComparer.OrdinalIgnoreCase);
        _sortedNames = _functions.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public bool TryGetFunction(string functionIdentifier, [MaybeNullWhen(false)] out FunctionDescriptor functionDescriptor)
    {
        return _functions.TryGetValue(functionIdentifier, out functionDescriptor);
    }

    public IEnumerable<FunctionDefinition> SearchForFunctions(string prefix)
    {
        prefix ??= string.Empty;

        foreach (var name in _sortedNames)
        {
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            yield return new FunctionDefinition(name, _functions[name]);
        }
    }
}
