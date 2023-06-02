using BlazorDatasheet.FormulaEngine.Interpreter.Functions;

namespace BlazorDatasheet.FormulaEngine;

public interface IEnvironment
{
    object? GetCellValue(int row, int col);
    List<double> GetNumbersInRange(RangeAddress rangeAddress);
    List<double> GetNumbersInRange(ColumnAddress rangeAddress);
    List<double> GetNumbersInRange(RowAddress rangeAddress);
    bool FunctionExists(string functionIdentifier);
    CallableFunctionDefinition GetFunctionDefinition(string identifierText);
    bool VariableExists(string variableIdentifier);
    object GetVariable(string variableIdentifier);
}