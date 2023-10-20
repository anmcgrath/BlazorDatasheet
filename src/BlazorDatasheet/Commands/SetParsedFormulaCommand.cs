using BlazorDatasheet.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Commands;

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
        _previousFormula = sheet.FormulaEngine.GetFormula(_row, _col);
        _previousValue = sheet.GetValue(_row, _col);
        sheet.FormulaEngine.SetFormula(_row, _col, _formula);
        if (_calculateSheetOnSet)
            sheet.FormulaEngine.CalculateSheet();
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_previousFormula == null)
        {
            sheet.FormulaEngine.ClearFormula(_row, _col);
            sheet.SetCellValueImpl(_row, _col, _previousValue);
        }
        else
            sheet.FormulaEngine.SetFormula(_row, _col, _previousFormula);
        if (_calculateSheetOnSet)
            sheet.FormulaEngine.CalculateSheet();
        return true;
    }
}