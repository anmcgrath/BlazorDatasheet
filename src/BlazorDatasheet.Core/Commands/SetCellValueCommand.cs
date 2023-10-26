using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

public class SetCellValueCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly object? _value;
    private CellStoreRestoreData _restoreData;

    public SetCellValueCommand(int row, int col, object? value)
    {
        _row = row;
        _col = col;
        _value = value;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.SetValueImpl(_row, _col, _value);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Selection.SetSingle(_row, _col);
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}