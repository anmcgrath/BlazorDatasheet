using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public interface IEnvironment : IFunctionProvider, IStructuredReferenceResolver
{
    CellValue GetCellValue(int row, int col, string sheetName);

    /// <summary>
    /// Return a cell formula at <paramref name="row"/>, <paramref name="col"/> if it exists.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="sheetName"></param>
    /// <returns></returns>
    CellFormula? GetFormula(int row, int col, string sheetName);

    public CellValue[][] GetRangeValues(Reference reference);

    /// <summary>
    /// The region that the array formula at <paramref name="row"/>, <paramref name="col"/> has spilled
    /// into, including the formula cell itself, or null if the cell has not spilled.
    /// </summary>
    IRegion? GetSpillRegion(int row, int col, string sheetName);
    bool VariableExists(string variableIdentifier);
    CellValue GetVariable(string variableIdentifier);
    bool TryGetVariable(string variableIdentifier, out CellValue value);
    void SetVariable(string name, CellValue value);
    public IEnumerable<CellValue> GetNonEmptyInRange(Reference reference);
    void SetCellValue(int row, int col, string sheetName, CellValue value);

    /// <summary>
    /// Writes an array result of the formula at <paramref name="row"/>, <paramref name="col"/>, spilling it
    /// into the cells below and to the right, or writing #SPILL! to the formula cell when they are not free.
    /// </summary>
    SpillResult SetSpillResult(int row, int col, string sheetName, CellValue[][] values);
    void ClearVariable(string varName);
    IEnumerable<string> GetVariableNames();
}
