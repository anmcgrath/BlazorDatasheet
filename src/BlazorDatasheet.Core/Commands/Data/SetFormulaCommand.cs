using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Commands.Data;

internal class SetFormulaCommand : BaseCommand, IUndoableCommand
{
    public int Row { get; }
    public int Col { get; }
    public string FormulaString { get; }
    private CellStoreRestoreData _restoreData = null!;

    public SetFormulaCommand(int row, int col, string formulaString)
    {
        Row = row;
        Col = col;
        FormulaString = formulaString;
    }

    public override bool CanExecute(Sheet sheet) => sheet.Region.Contains(Row, Col);

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.SetFormulaImpl(Row, Col, FormulaString);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}