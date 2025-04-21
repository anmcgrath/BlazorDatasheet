namespace BlazorDatasheet.Formula.Core;

public class FunctionDefinition
{
    public string Name { get; }
    public ISheetFunction Function { get; }

    public FunctionDefinition(string name, ISheetFunction function)
    {
        Name = name;
        Function = function;
    }
}