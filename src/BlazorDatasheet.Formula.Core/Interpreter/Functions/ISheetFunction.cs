namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public interface ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions();
    public object? Call(CellValue[] args, FunctionCallMetaData metaData);
    public bool AcceptsErrors { get; }
}