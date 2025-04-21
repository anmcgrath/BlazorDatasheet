namespace BlazorDatasheet.Formula.Core;

public interface IFunctionProvider
{
    bool FunctionExists(string functionIdentifier);
    ISheetFunction? GetFunctionDefinition(string identifierText);
    void RegisterFunction(string name, ISheetFunction value);
    IEnumerable<FunctionDefinition> SearchForFunctions(string functionName);
}