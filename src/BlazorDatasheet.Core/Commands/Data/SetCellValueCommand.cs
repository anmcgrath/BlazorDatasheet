using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValueCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly CellValue? _valueAsCellValue;
    private readonly object? _value;
    private CellStoreRestoreData _restoreData = null!;

    /// <summary>
    /// Sets a single cell value to the <paramref name="value"/>. No conversion is performed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    public SetCellValueCommand(int row, int col, CellValue value)
    {
        _row = row;
        _col = col;
        _valueAsCellValue = value;
    }

    /// <summary>
    /// Sets a single cell value to the <paramref name="value"/>. Conversion to <seealso cref="CellValue"/> is performed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    public SetCellValueCommand(int row, int col, object? value)
    {
        _row = row;
        _col = col;
        _value = value;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;

        if (_valueAsCellValue != null)
            _restoreData = sheet.Cells.SetValueImpl(_row, _col, _valueAsCellValue);
        else
            _restoreData = sheet.Cells.SetValueImpl(_row, _col, _value);

        sheet.MarkDirty(_row, _col);
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