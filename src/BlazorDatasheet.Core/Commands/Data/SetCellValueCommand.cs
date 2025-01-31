using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValueCommand : BaseCommand, IUndoableCommand
{
    public int Row { get; }
    public int Column { get; }
    public CellValue Value { get; }

    /// <summary>
    /// We need this so that we respect type in conversion
    /// </summary>
    public object? RawValue { get; }

    private CellStoreRestoreData _restoreData = null!;

    /// <summary>
    /// Sets a single cell value to the <paramref name="value"/>. No conversion is performed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    /// <param name="value"></param>
    public SetCellValueCommand(int row, int column, CellValue value)
    {
        Row = row;
        Column = column;
        Value = value;
    }

    /// <summary>
    /// Sets a single cell value to the <paramref name="value"/>. Conversion to <seealso cref="CellValue"/> is performed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    /// <param name="value"></param>
    public SetCellValueCommand(int row, int column, object? value)
    {
        Row = row;
        Column = column;
        RawValue = value;
    }

    public override bool CanExecute(Sheet sheet) => sheet.Region.Contains(Row, Column);

    public override bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;

        if (RawValue != null)
            _restoreData = sheet.Cells.SetValueImpl(Row, Column, RawValue);
        else
            _restoreData = sheet.Cells.SetValueImpl(Row, Column, Value);

        sheet.MarkDirty(Row, Column);
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