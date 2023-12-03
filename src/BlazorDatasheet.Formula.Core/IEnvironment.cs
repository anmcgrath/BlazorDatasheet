using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Formula.Core;

public interface IEnvironment
{
    CellValue GetCellValue(int row, int col);
    public CellValue[][] GetRangeValues(RangeAddress rangeAddress);
    bool FunctionExists(string functionIdentifier);
    ISheetFunction GetFunctionDefinition(string identifierText);
    bool VariableExists(string variableIdentifier);
    object GetVariable(string variableIdentifier);
    void RegisterFunction(string name, ISheetFunction value);
}