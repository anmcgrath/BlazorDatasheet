using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public interface IEnvironment
{
    CellValue GetCellValue(int row, int col);

    /// <summary>
    /// Return a cell formula at <paramref name="row"/>, <paramref name="col"/> if it exists.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    CellFormula? GetFormula(int row, int col);

    public CellValue[][] GetRangeValues(Reference reference);
    bool FunctionExists(string functionIdentifier);
    ISheetFunction GetFunctionDefinition(string identifierText);
    bool VariableExists(string variableIdentifier);
    CellValue GetVariable(string variableIdentifier);
    void SetVariable(string name, CellValue value);
    void RegisterFunction(string name, ISheetFunction value);
    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference);
}