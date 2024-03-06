using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public interface IEnvironment
{
    CellValue GetCellValue(int row, int col);
    public CellValue[][] GetRangeValues(Reference reference);
    bool FunctionExists(string functionIdentifier);
    ISheetFunction GetFunctionDefinition(string identifierText);
    bool VariableExists(string variableIdentifier);
    CellValue GetVariable(string variableIdentifier);
    void SetVariable(string name, CellValue value);
    void RegisterFunction(string name, ISheetFunction value);
    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference);
}