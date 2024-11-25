using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Commands.Data;

internal class SetParsedFormulaCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly CellFormula _formula;
    private CellStoreRestoreData _restoreData = null!;

    public SetParsedFormulaCommand(int row, int col, CellFormula formula)
    {
        _row = row;
        _col = col;
        _formula = formula;
    }

    public bool CanExecute(Sheet sheet) => sheet.Region.Contains(_row, _col);

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.SetFormulaImpl(_row, _col, _formula);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}