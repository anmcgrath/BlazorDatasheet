using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

internal class SetParsedFormulaCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly CellFormula _formula;
    private readonly bool _calculateSheetOnSet;
    private CellFormula? _previousFormula;
    private object? _previousValue;

    public SetParsedFormulaCommand(int row, int col, CellFormula formula, bool calculateSheetOnSet = false)
    {
        _row = row;
        _col = col;
        _formula = formula;
        _calculateSheetOnSet = calculateSheetOnSet;
    }

    public bool Execute(Sheet sheet)
    {
        _previousFormula = sheet.CellFormulaStore.Get(_row, _col);

        if (_previousFormula?.ToFormulaString() == _formula.ToFormulaString())
            return false;

        _previousValue = sheet.GetValue(_row, _col);
        sheet.CellFormulaStore.Set(_row, _col, _formula);
        sheet.FormulaEngine.AddToDependencyGraph(_row, _col, _formula);
        
        if (_calculateSheetOnSet)
            sheet.FormulaEngine.CalculateSheet();

        sheet.MarkDirty(_row, _col);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_previousFormula == null)
        {
            sheet.CellFormulaStore.Clear(_row, _col);
            sheet.FormulaEngine.RemoveFromDependencyGraph(_row, _col);
            sheet.CellDataStore.Set(_row, _col, _previousValue);
        }
        else
        {
            sheet.CellFormulaStore.Set(_row, _col, _previousFormula);
            sheet.FormulaEngine.AddToDependencyGraph(_row, _col, _previousFormula);
        }

        sheet.MarkDirty(_row, _col);

        if (_calculateSheetOnSet)
            sheet.FormulaEngine.CalculateSheet();
        return true;
    }
}