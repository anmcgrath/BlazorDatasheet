using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public class FunctionCallMetaData
{
    internal FunctionCallMetaData(ParameterDefinition[] parameterDefinitions, CellReference? callingCell = null)
    {
        ParameterDefinitions = parameterDefinitions;
        CallingCell = callingCell;
    }

    public ParameterDefinition[] ParameterDefinitions { get; }
    public CellReference? CallingCell { get; }
    public int? CallingRowIndex => CallingCell?.RowIndex;
    public int? CallingColumnIndex => CallingCell?.ColIndex;
    public string? CallingSheetName => CallingCell?.SheetName;
}
