namespace BlazorDatasheet.Formula.Core;

public interface ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions();
    public object? Call(CellValue[] args, FunctionCallMetaData metaData);
    public bool AcceptsErrors { get; }
}