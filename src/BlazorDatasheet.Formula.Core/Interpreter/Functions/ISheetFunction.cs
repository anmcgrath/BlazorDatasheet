namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public interface ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions();
    public object Call(FuncArg[] args);
    public bool AcceptsErrors { get; }
}