using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatasheet.Formula.Core;

public interface IEnvironment
{
    CellValue GetCellValue(int row, int col);
    public CellValue[][] GetRangeValues(RangeAddress rangeAddress);
    public CellValue[][] GetRangeValues(ColumnAddress rangeAddress);
    /// <summary>
    /// Returns the row/column data in the range.
    /// Accessible by data[row][col]
    /// </summary>
    /// <param name="rangeAddress"></param>
    /// <returns></returns>
    public CellValue[][] GetRangeValues(RowAddress rangeAddress);
    
    bool FunctionExists(string functionIdentifier);
    ISheetFunction GetFunctionDefinition(string identifierText);
    bool VariableExists(string variableIdentifier);
    object GetVariable(string variableIdentifier);
    void SetFunction(string name, ISheetFunction value);
}