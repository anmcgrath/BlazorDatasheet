﻿using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Commands.Data;

internal class SetFormulaCommand : BaseCommand, IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly string _formula;
    private CellStoreRestoreData _restoreData = null!;

    public SetFormulaCommand(int row, int col, string formula)
    {
        _row = row;
        _col = col;
        _formula = formula;
    }

    public override bool CanExecute(Sheet sheet) => sheet.Region.Contains(_row, _col);

    public override bool Execute(Sheet sheet)
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