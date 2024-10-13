using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValueCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly CellValue _value;
    private CellStoreRestoreData _restoreData = null!;

    public SetCellValueCommand(int row, int col, CellValue value)
    {
        _row = row;
        _col = col;
        _value = value;
    }

    public SetCellValueCommand(int row, int col, object? value) : this(row, col, new CellValue(value))
    {
    }

    public bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        _restoreData = sheet.Cells.SetValueImpl(_row, _col, _value);
        sheet.ScreenUpdating = true;
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.Cells.Restore(_restoreData);
        sheet.ScreenUpdating = true;
        return true;
    }
}