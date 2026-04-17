namespace BlazorDatasheet.Formula.Core;

public class FunctionDefinition
{
    public string Name { get; }
    public FunctionDescriptor Function { get; }

    public FunctionDefinition(string name, FunctionDescriptor function)
    {
        Name = name;
        Function = function;
    }
}
