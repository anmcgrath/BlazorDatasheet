using System.Diagnostics.CodeAnalysis;

namespace BlazorDatasheet.Formula.Core;

public interface IFunctionProvider
{
    bool TryGetFunction(string functionIdentifier, [MaybeNullWhen(false)] out FunctionDescriptor functionDescriptor);
    IEnumerable<FunctionDefinition> SearchForFunctions(string functionName);
}
